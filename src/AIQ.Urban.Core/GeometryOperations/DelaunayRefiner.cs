using AIQ.Urban.Core.Extensions;
using Rhino.Geometry;

namespace AIQ.Urban.Core.GeometryOperations;

internal static class DelaunayRefiner
{
    public static Mesh Refine(Mesh mesh, double tolerance = 1e-5)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        if (mesh.Faces.Count == 0)
            return new Mesh();

        CheckAllFacesAreTriangles(mesh);
        CheckMeshIsPlanarXY(mesh, tolerance);

        var vertexPositions = CopyVertices(mesh);
        var triangleFaces = CopyTriangleFaces(mesh);
        var faceCount = triangleFaces.Length;

        var maxPasses = faceCount * faceCount;
        for (int pass = 0; pass < maxPasses; pass++)
        {
            var edgeToFaces = BuildEdgeToFacesMap(triangleFaces, faceCount);
            var flippedAny = false;

            foreach (var edgeEntry in edgeToFaces)
            {
                var facesSharingEdge = edgeEntry.Value;
                if (facesSharingEdge.Count != 2)
                    continue;

                var faceA = facesSharingEdge[0];
                var faceB = facesSharingEdge[1];
                var (edgeV0, edgeV1) = edgeEntry.Key;

                var oppositeA = FindOppositeVertex(triangleFaces[faceA], edgeV0, edgeV1);
                var oppositeB = FindOppositeVertex(triangleFaces[faceB], edgeV0, edgeV1);

                // Determine CCW edge direction within faceA for correct quadrilateral ordering
                int ccwV0, ccwV1;
                if (HasEdge(triangleFaces[faceA], edgeV0, edgeV1))
                {
                    ccwV0 = edgeV0;
                    ccwV1 = edgeV1;
                }
                else
                {
                    ccwV0 = edgeV1;
                    ccwV1 = edgeV0;
                }

                if (!IsConvex(
                        vertexPositions[oppositeA],
                        vertexPositions[ccwV0],
                        vertexPositions[oppositeB],
                        vertexPositions[ccwV1]))
                    continue;

                if (!vertexPositions[oppositeB].IsInsideCircumcircle(
                        vertexPositions[oppositeA],
                        vertexPositions[edgeV0],
                        vertexPositions[edgeV1],
                        tolerance))
                    continue;

                triangleFaces[faceA] = new Face(oppositeA, ccwV0, oppositeB);
                triangleFaces[faceB] = new Face(oppositeA, oppositeB, ccwV1);
                flippedAny = true;
                break;
            }

            if (!flippedAny)
                break;
        }

        return BuildMesh(vertexPositions, triangleFaces);
    }

    private static void CheckAllFacesAreTriangles(Mesh mesh)
    {
        for (int i = 0; i < mesh.Faces.Count; i++)
        {
            if (!mesh.Faces[i].IsTriangle)
                throw new ArgumentException("All faces must be triangles");
        }
    }

    private static void CheckMeshIsPlanarXY(Mesh mesh, double tolerance)
    {
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            if (Math.Abs(mesh.Vertices[i].Z) > tolerance)
                throw new ArgumentException("Mesh must be planar at z=0");
        }
    }

    private static Point3d[] CopyVertices(Mesh mesh)
    {
        var vertices = new Point3d[mesh.Vertices.Count];
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = mesh.Vertices[i];
        return vertices;
    }

    private static Face[] CopyTriangleFaces(Mesh mesh)
    {
        var faces = new Face[mesh.Faces.Count];
        var writeIndex = 0;
        for (int i = 0; i < faces.Length; i++)
        {
            var f = mesh.Faces[i];
            if (f.A != f.B && f.B != f.C && f.C != f.A)
                faces[writeIndex++] = new Face(f.A, f.B, f.C);
        }

        if (writeIndex < faces.Length)
            Array.Resize(ref faces, writeIndex);

        return faces;
    }

    private static Dictionary<(int, int), List<int>> BuildEdgeToFacesMap(Face[] faces, int faceCount)
    {
        var edgeToFaces = new Dictionary<(int, int), List<int>>();
        for (int i = 0; i < faceCount; i++)
        {
            var f = faces[i];
            AddEdgeToMap(edgeToFaces, i, f.V0, f.V1);
            AddEdgeToMap(edgeToFaces, i, f.V1, f.V2);
            AddEdgeToMap(edgeToFaces, i, f.V2, f.V0);
        }
        return edgeToFaces;
    }

    private static void AddEdgeToMap(Dictionary<(int, int), List<int>> edgeToFaces, int faceIndex, int vertexA, int vertexB)
    {
        var key = vertexA < vertexB ? (vertexA, vertexB) : (vertexB, vertexA);
        if (!edgeToFaces.TryGetValue(key, out var faceList))
        {
            faceList = [];
            edgeToFaces[key] = faceList;
        }
        faceList.Add(faceIndex);
    }

    private static int FindOppositeVertex(Face face, int vertexA, int vertexB)
    {
        if (face.V0 != vertexA && face.V0 != vertexB) return face.V0;
        if (face.V1 != vertexA && face.V1 != vertexB) return face.V1;
        return face.V2;
    }

    private static bool HasEdge(Face face, int vertexA, int vertexB)
    {
        return (face.V0 == vertexA && face.V1 == vertexB) ||
               (face.V1 == vertexA && face.V2 == vertexB) ||
               (face.V2 == vertexA && face.V0 == vertexB);
    }

    private static bool IsConvex(Point3d a, Point3d b, Point3d c, Point3d d)
    {
        return a.CrossZ(b, c) > 0 &&
               b.CrossZ(c, d) > 0 &&
               c.CrossZ(d, a) > 0 &&
               d.CrossZ(a, b) > 0;
    }

    private static Mesh BuildMesh(Point3d[] vertices, Face[] faces)
    {
        var mesh = new Mesh();
        for (int i = 0; i < vertices.Length; i++)
            mesh.Vertices.Add((Point3f)vertices[i]);

        for (int i = 0; i < faces.Length; i++)
            mesh.Faces.AddFace(faces[i].V0, faces[i].V1, faces[i].V2, faces[i].V2);

        return mesh;
    }
}

internal readonly struct Face(int v0, int v1, int v2)
{
    public readonly int V0 = v0;
    public readonly int V1 = v1;
    public readonly int V2 = v2;
}
