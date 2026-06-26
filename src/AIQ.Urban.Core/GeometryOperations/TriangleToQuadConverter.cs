using Rhino.Geometry;

namespace AIQ.Urban.Core.GeometryOperations;

internal static class TriangleToQuadConverter
{
    public static Mesh Convert(Mesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        if (mesh.Faces.Count == 0)
            return new Mesh();

        if (mesh.Faces.Any(f => !f.IsTriangle))
            throw new ArgumentException("All faces must be triangles");

        if (mesh.Vertices.Any(v => Math.Abs(v.Z) > 1e-6))
            throw new ArgumentException("Mesh must be planar at z=0");

        if (mesh.Faces.Count <= 1)
            return mesh.DuplicateMesh();

        var edgeToTris = BuildEdgeMap(mesh);
        var neighbors = BuildNeighborLists(mesh.Faces.Count, edgeToTris);
        var merged = new bool[mesh.Faces.Count];
        var quads = new List<(int, int, int, int)>();
        var leftoverTris = new List<(int, int, int)>();

        for (var i = 0; i < mesh.Faces.Count; i++)
        {
            if (merged[i])
                continue;

            var bestPair = FindBestMergePair(i, mesh, neighbors, merged);

            if (bestPair.HasValue)
            {
                var (neighborIdx, uA, s0, s1, uB) = bestPair.Value;
                quads.Add((uA, s0, uB, s1));
                merged[i] = true;
                merged[neighborIdx] = true;
            }
            else
            {
                var f = mesh.Faces[i];
                leftoverTris.Add((f.A, f.B, f.C));
            }
        }

        return BuildResultMesh(mesh, quads, leftoverTris);
    }

    private static Dictionary<(int, int), List<int>> BuildEdgeMap(Mesh mesh)
    {
        var map = new Dictionary<(int, int), List<int>>();

        for (var i = 0; i < mesh.Faces.Count; i++)
        {
            var f = mesh.Faces[i];
            AddEdge(map, i, f.A, f.B);
            AddEdge(map, i, f.B, f.C);
            AddEdge(map, i, f.C, f.A);
        }

        return map;
    }

    private static void AddEdge(Dictionary<(int, int), List<int>> map, int faceIdx, int v0, int v1)
    {
        var key = v0 < v1 ? (v0, v1) : (v1, v0);

        if (!map.TryGetValue(key, out var list))
        {
            list = [];
            map[key] = list;
        }

        list.Add(faceIdx);
    }

    private static List<int>[] BuildNeighborLists(int faceCount, Dictionary<(int, int), List<int>> edgeToTris)
    {
        var neighbors = new List<int>[faceCount];

        for (var i = 0; i < faceCount; i++)
            neighbors[i] = [];

        foreach (var kvp in edgeToTris)
        {
            if (kvp.Value.Count != 2)
                continue;

            var t0 = kvp.Value[0];
            var t1 = kvp.Value[1];
            neighbors[t0].Add(t1);
            neighbors[t1].Add(t0);
        }

        return neighbors;
    }

    private static (int neighborIdx, int uA, int s0, int s1, int uB)? FindBestMergePair(
        int triIdx, Mesh mesh, List<int>[] neighbors, bool[] merged)
    {
        var tri = mesh.Faces[triIdx];
        var bestScore = double.MaxValue;
        (int, int, int, int, int)? bestPair = null;

        foreach (var neighborIdx in neighbors[triIdx])
        {
            if (merged[neighborIdx])
                continue;

            if (TryGetSharedEdge(tri, mesh.Faces[neighborIdx], out var s0, out var s1, out var uA, out var uB))
            {
                if (!IsQuadConvex(mesh, uA, s0, uB, s1))
                    continue;

                var d1 = mesh.Vertices[s0].DistanceTo(mesh.Vertices[s1]);
                var d2 = mesh.Vertices[uA].DistanceTo(mesh.Vertices[uB]);
                var min = Math.Min(d1, d2);
                var max = Math.Max(d1, d2);

                if (min < 1e-10)
                    continue;

                var score = max / min;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPair = (neighborIdx, uA, s0, s1, uB);
                }
            }
        }

        return bestPair;
    }

    private static bool TryGetSharedEdge(
        MeshFace a, MeshFace b,
        out int s0, out int s1,
        out int uA, out int uB)
    {
        var aVerts = new[] { a.A, a.B, a.C };
        var bVerts = new[] { b.A, b.B, b.C };

        s0 = -1;
        s1 = -1;
        uA = -1;
        uB = -1;
        var sharedCount = 0;

        foreach (var va in aVerts)
        {
            if (bVerts.Contains(va))
            {
                if (sharedCount == 0)
                    s0 = va;
                else
                    s1 = va;
                sharedCount++;
            }
            else
            {
                uA = va;
            }
        }

        foreach (var vb in bVerts)
        {
            if (!aVerts.Contains(vb))
                uB = vb;
        }

        return sharedCount == 2;
    }

    private static bool IsQuadConvex(Mesh mesh, int a, int b, int c, int d)
    {
        var pa = mesh.Vertices[a];
        var pb = mesh.Vertices[b];
        var pc = mesh.Vertices[c];
        var pd = mesh.Vertices[d];

        var area = 0.5 * (
            pa.X * pb.Y - pb.X * pa.Y +
            pb.X * pc.Y - pc.X * pb.Y +
            pc.X * pd.Y - pd.X * pc.Y +
            pd.X * pa.Y - pa.X * pd.Y);

        return Math.Abs(area) > 1e-10;
    }

    private static Mesh BuildResultMesh(
        Mesh source,
        List<(int, int, int, int)> quads,
        List<(int, int, int)> tris)
    {
        var result = new Mesh();
        result.Vertices.AddVertices(source.Vertices);

        foreach (var (a, b, c, d) in quads)
            result.Faces.AddFace(a, b, c, d);

        foreach (var (a, b, c) in tris)
            result.Faces.AddFace(a, b, c, c);

        result.Normals.ComputeNormals();
        return result;
    }
}
