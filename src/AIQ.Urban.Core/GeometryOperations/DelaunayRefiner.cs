using Rhino.Geometry;

namespace AIQ.Urban.Core.GeometryOperations;

internal static class DelaunayRefiner
{
    public static Mesh Refine(Mesh mesh, double tolerance = 1e-5)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        var result = new Mesh();
        if (mesh.Faces.Count == 0)
            return result;

        for (var i = 0; i < mesh.Faces.Count; i++)
        {
            if (!mesh.Faces[i].IsTriangle)
                throw new ArgumentException("All faces must be triangles");
        }

        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            if (System.Math.Abs(mesh.Vertices[i].Z) > tolerance)
                throw new ArgumentException("Mesh must be planar at z=0");
        }

        var verts = new Point3d[mesh.Vertices.Count];
        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            verts[i] = new Point3d(v.X, v.Y, 0);
        }

        var nFaces = mesh.Faces.Count;
        var rawTris = new (int A, int B, int C)[nFaces];
        var nValid = 0;
        for (var i = 0; i < nFaces; i++)
        {
            var f = mesh.Faces[i];
            if (f.A != f.B && f.B != f.C && f.C != f.A)
                rawTris[nValid++] = (f.A, f.B, f.C);
        }
        var triVerts = rawTris[..nValid];
        nFaces = nValid;

        var adj = BuildAdjacency(triVerts, nFaces);

        var maxPasses = nFaces * nFaces;
        for (var pass = 0; pass < maxPasses; pass++)
        {
            var flippedAny = false;

            for (var fIdx = 0; fIdx < nFaces; fIdx++)
            {
                var (a, b, c) = triVerts[fIdx];

                for (var edge = 0; edge < 3; edge++)
                {
                    var nIdx = adj[fIdx][edge];
                    if (nIdx < 0 || nIdx <= fIdx) continue;

                    var vertsInEdge = GetEdgeVerts(fIdx, edge, triVerts);
                    var (v0, v1) = vertsInEdge;

                    var opp0 = OppositeVert(fIdx, v0, v1, triVerts);
                    var opp1 = OppositeVert(nIdx, v0, v1, triVerts);

                    if (opp0 < 0 || opp1 < 0) continue;

                    if (!IsConvex(verts[opp0], verts[v0], verts[opp1], verts[v1], tolerance))
                        continue;

                    if (!InCircle(verts[opp0], verts[v0], verts[v1], verts[opp1], tolerance))
                        continue;

                    triVerts[fIdx] = (opp0, v0, opp1);
                    triVerts[nIdx] = (opp1, v1, opp0);

                    adj[fIdx] = BuildFaceAdjacency(fIdx, triVerts, nFaces);
                    adj[nIdx] = BuildFaceAdjacency(nIdx, triVerts, nFaces);

                    UpdateNeighborReferences(adj, fIdx, nIdx, triVerts, nFaces);

                    flippedAny = true;
                    break;
                }
            }

            if (!flippedAny) break;
        }

        for (var i = 0; i < verts.Length; i++)
            result.Vertices.Add((Point3f)verts[i]);

        for (var i = 0; i < nFaces; i++)
        {
            var (a, b, c) = triVerts[i];
            result.Faces.AddFace(a, b, c, c);
        }

        return result;
    }

    private static int[][] BuildAdjacency((int A, int B, int C)[] triVerts, int nFaces)
    {
        var edgeToFaces = new Dictionary<(int, int), List<int>>();
        for (var i = 0; i < nFaces; i++)
        {
            var (a, b, c) = triVerts[i];
            AddEdge(edgeToFaces, i, a, b);
            AddEdge(edgeToFaces, i, b, c);
            AddEdge(edgeToFaces, i, c, a);
        }

        var adj = new int[nFaces][];
        for (var i = 0; i < nFaces; i++)
            adj[i] = [-1, -1, -1];

        foreach (var kvp in edgeToFaces)
        {
            var faces = kvp.Value;
            if (faces.Count == 2)
            {
                var f0 = faces[0];
                var f1 = faces[1];
                var (v0, v1) = kvp.Key;
                var edge0 = EdgeIndex(f0, v0, v1, triVerts);
                var edge1 = EdgeIndex(f1, v0, v1, triVerts);
                adj[f0][edge0] = f1;
                adj[f1][edge1] = f0;
            }
        }

        return adj;
    }

    private static void AddEdge(Dictionary<(int, int), List<int>> dict, int faceIdx, int v0, int v1)
    {
        var key = v0 < v1 ? (v0, v1) : (v1, v0);
        if (!dict.TryGetValue(key, out var list))
        {
            list = [];
            dict[key] = list;
        }
        list.Add(faceIdx);
    }

    private static int EdgeIndex(int faceIdx, int v0, int v1, (int A, int B, int C)[] triVerts)
    {
        var (a, b, c) = triVerts[faceIdx];
        if ((a == v0 && b == v1) || (a == v1 && b == v0)) return 0;
        if ((b == v0 && c == v1) || (b == v1 && c == v0)) return 1;
        if ((c == v0 && a == v1) || (c == v1 && a == v0)) return 2;
        return -1;
    }

    private static (int v0, int v1) GetEdgeVerts(int faceIdx, int edgeIdx, (int A, int B, int C)[] triVerts)
    {
        var (a, b, c) = triVerts[faceIdx];
        return edgeIdx switch
        {
            0 => (a, b),
            1 => (b, c),
            2 => (c, a),
            _ => (0, 0),
        };
    }

    private static int OppositeVert(int faceIdx, int v0, int v1, (int A, int B, int C)[] triVerts)
    {
        var (a, b, c) = triVerts[faceIdx];
        if ((a != v0 && a != v1)) return a;
        if ((b != v0 && b != v1)) return b;
        if ((c != v0 && c != v1)) return c;
        return -1;
    }

    private static int[] BuildFaceAdjacency(int faceIdx, (int A, int B, int C)[] triVerts, int nFaces)
    {
        var result = new[] { -1, -1, -1 };
        var (a, b, c) = triVerts[faceIdx];
        var edges = new[] { (a, b), (b, c), (c, a) };

        for (var e = 0; e < 3; e++)
        {
            var (v0, v1) = edges[e];
            for (var j = 0; j < nFaces; j++)
            {
                if (j == faceIdx) continue;
                var (ja, jb, jc) = triVerts[j];
                if (SharesEdge(v0, v1, ja, jb, jc))
                {
                    result[e] = j;
                    break;
                }
            }
        }

        return result;
    }

    private static bool SharesEdge(int v0, int v1, int a, int b, int c)
    {
        return (a == v0 && b == v1) || (a == v1 && b == v0) ||
               (b == v0 && c == v1) || (b == v1 && c == v0) ||
               (c == v0 && a == v1) || (c == v1 && a == v0);
    }

    private static void UpdateNeighborReferences(int[][] adj, int oldF, int oldN, (int A, int B, int C)[] triVerts, int nFaces)
    {
        for (var i = 0; i < nFaces; i++)
        {
            if (i == oldF || i == oldN) continue;
            var oldAdj = adj[i];
            for (var e = 0; e < 3; e++)
            {
                if (oldAdj[e] == oldF || oldAdj[e] == oldN)
                {
                    adj[i] = BuildFaceAdjacency(i, triVerts, nFaces);
                    break;
                }
            }
        }
    }

    private static bool IsConvex(Point3d a, Point3d b, Point3d c, Point3d d, double tolerance)
    {
        var c1 = CrossZ(a, b, c);
        var c2 = CrossZ(b, c, d);
        var c3 = CrossZ(c, d, a);
        var c4 = CrossZ(d, a, b);
        return (c1 > -tolerance && c2 > -tolerance && c3 > -tolerance && c4 > -tolerance) ||
               (c1 < tolerance && c2 < tolerance && c3 < tolerance && c4 < tolerance);
    }

    private static double CrossZ(Point3d p, Point3d q, Point3d r)
    {
        return (q.X - p.X) * (r.Y - q.Y) - (q.Y - p.Y) * (r.X - q.X);
    }

    private static bool InCircle(Point3d a, Point3d b, Point3d c, Point3d d, double tolerance)
    {
        var ax = a.X; var ay = a.Y;
        var bx = b.X; var by = b.Y;
        var cx = c.X; var cy = c.Y;
        var dx = d.X; var dy = d.Y;

        var a2 = ax * ax + ay * ay;
        var b2 = bx * bx + by * by;
        var c2 = cx * cx + cy * cy;
        var d2 = dx * dx + dy * dy;

        var det = (ax * (by * c2 - b2 * cy) - ay * (bx * c2 - b2 * cx) + a2 * (bx * cy - by * cx))
                - (ax * (by * d2 - b2 * dy) - ay * (bx * d2 - b2 * dx) + a2 * (bx * dy - by * dx))
                + (ax * (cy * d2 - c2 * dy) - ay * (cx * d2 - c2 * dx) + a2 * (cx * dy - cy * dx))
                - (bx * (cy * d2 - c2 * dy) - by * (cx * d2 - c2 * dx) + b2 * (cx * dy - cy * dx));

        return det > tolerance;
    }
}
