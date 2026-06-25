using Rhino.Geometry;

namespace AIQ.Urban.Core.GeometryOperations;

internal static class DelaunayRefiner
{
    public static Mesh Refine(Mesh mesh, double tolerance = 1e-5)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        if (mesh.Faces.Count == 0)
            return new Mesh();

        for (var i = 0; i < mesh.Faces.Count; i++)
        {
            if (!mesh.Faces[i].IsTriangle)
                throw new ArgumentException("All faces must be triangles");
        }

        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            if (Math.Abs(mesh.Vertices[i].Z) > tolerance)
                throw new ArgumentException("Mesh must be planar at z=0");
        }

        var verts = new Point3d[mesh.Vertices.Count];
        for (var i = 0; i < mesh.Vertices.Count; i++)
            verts[i] = mesh.Vertices[i];

        var nFaces = mesh.Faces.Count;
        var triVerts = new (int A, int B, int C)[nFaces];
        for (var i = 0; i < nFaces; i++)
        {
            var f = mesh.Faces[i];
            triVerts[i] = (f.A, f.B, f.C);
        }

        triVerts = triVerts
            .Where(t => t.A != t.B && t.B != t.C && t.C != t.A)
            .ToArray();
        nFaces = triVerts.Length;

        if (nFaces == 0)
            return new Mesh();

        var adj = BuildAdjacency(triVerts, nFaces);
        var maxPasses = nFaces * nFaces;

        for (var pass = 0; pass < maxPasses; pass++)
        {
            var flippedAny = false;

            for (var fIdx = 0; fIdx < nFaces && !flippedAny; fIdx++)
            {
                var fAdj = adj[fIdx];
                for (var e = 0; e < 3; e++)
                {
                    var nIdx = fAdj[e];
                    if (nIdx < 0 || nIdx <= fIdx) continue;

                    var (v0, v1) = SharedEdgeVertices(triVerts[fIdx], triVerts[nIdx], e);
                    var opp0 = OppositeVertex(triVerts[fIdx], v0, v1);
                    var opp1 = OppositeVertex(triVerts[nIdx], v0, v1);

                    if (!IsConvex(verts[opp0], verts[v0], verts[v1], verts[opp1]))
                        continue;

                    if (!InCircle(verts[opp0], verts[v0], verts[v1], verts[opp1], tolerance))
                        continue;

                    FlipEdge(fIdx, nIdx, opp0, v0, v1, opp1, ref triVerts);
                    adj = BuildAdjacency(triVerts, nFaces);
                    flippedAny = true;
                    break;
                }
            }

            if (!flippedAny)
                break;
        }

        var result = new Mesh();
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
        if ((a == v0 && b == v1) || (a == v1 && b == v0)) return 2;
        if ((b == v0 && c == v1) || (b == v1 && c == v0)) return 0;
        return 1;
    }

    private static (int V0, int V1) SharedEdgeVertices((int A, int B, int C) t1, (int A, int B, int C) t2, int e1)
    {
        return e1 switch
        {
            0 => (t1.B, t1.C),
            1 => (t1.C, t1.A),
            _ => (t1.A, t1.B),
        };
    }

    private static int OppositeVertex((int A, int B, int C) tri, int v0, int v1)
    {
        if (tri.A != v0 && tri.A != v1) return tri.A;
        if (tri.B != v0 && tri.B != v1) return tri.B;
        return tri.C;
    }

    private static bool IsConvex(Point3d a, Point3d b, Point3d c, Point3d d)
    {
        return CrossZ(a, b, c) > 0 && CrossZ(b, c, d) > 0 &&
               CrossZ(c, d, a) > 0 && CrossZ(d, a, b) > 0;
    }

    private static double CrossZ(Point3d p, Point3d q, Point3d r)
    {
        return (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
    }

    private static void FlipEdge(
        int f0, int f1,
        int opp0, int v0, int v1, int opp1,
        ref (int A, int B, int C)[] triVerts)
    {
        triVerts[f0] = (opp0, v0, opp1);
        triVerts[f1] = (opp0, opp1, v1);
    }

    private static bool InCircle(Point3d a, Point3d b, Point3d c, Point3d d, double tolerance)
    {
        if (CrossZ(a, b, c) < 0)
            (b, c) = (c, b);

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
