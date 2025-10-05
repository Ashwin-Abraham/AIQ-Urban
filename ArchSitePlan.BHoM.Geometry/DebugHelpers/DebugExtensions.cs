using ArchSitePlan.BHoM.Geometry.Models;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using Elements;

namespace ArchSitePlan.BHoM.Geometry.DebugHelpers;
public static class DebugExtensions
{
    public static List<Element> Debug(this ColoredMesh mesh)
    {
        var hyparVertices = mesh.Mesh.Vertices
            .Select(v => new Elements.Geometry.Vertex(v.ToHypar()))
            .ToList();

        var faces = mesh.Mesh.Faces
            .Select(x => x.ToHypar(mesh.Mesh, hyparVertices).ToList())
            .Select((triangles, faceIndex) => (triangles, faceIndex))
            .ToList();

        var meshFaceMaterials = mesh.FaceColors
            .Select(x => new Material("", new Elements.Geometry.Color(x)))
            .ToList();

        var elements = new List<Element>();

        foreach (var face in faces)
        {
            foreach (var triangle in face.triangles)
            {
                var faceMaterial = meshFaceMaterials[face.faceIndex];
                var triElement = triangle.DebugWithColours(faceMaterial);
                elements.Add(triElement);
            }
        }

        return elements;
    }




   
}
