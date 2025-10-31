#if DEBUG

using ArchSitePlan.BHoM.Geometry.Models;
using ArchSitePlan.DebugModel.BHoMExtensions;

namespace ArchSitePlan.BHoM.Geometry.DebugHelpers;
public static class ToHyparExtensions
{
    public static Elements.Geometry.Mesh ToHypar(this ColoredMesh mesh)
    {
        var hyparVertices = mesh.Mesh.Vertices
            .Select(v => new Elements.Geometry.Vertex(v.ToHypar()))
            .ToList();

        var vertexColors = mesh.GetVertexColors();

        for (int i = 0; i < hyparVertices.Count; i++)
        {
            hyparVertices[i].Color = new Elements.Geometry.Color(vertexColors[i]);
        }

        var triangles = new List<Elements.Geometry.Triangle>();

        for (int i = 0; i < mesh.Mesh.Faces.Count; i++)
        {
            var face = mesh.Mesh.Faces[i];
            var color = mesh.FaceColors[i];
            var faceTriangles = face.ToHypar(mesh.Mesh, hyparVertices).ToList();
            triangles.AddRange(faceTriangles);
        }

        return new Elements.Geometry.Mesh(hyparVertices, triangles);
    }
}
#endif