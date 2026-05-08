using AIQ.Primitives.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using System.Drawing;

namespace ArchSitePlan.BHoM.Geometry.Models;
public class ColoredMesh
{
    public Mesh Mesh { get; set; }
    public List<Color> FaceColors { get; set; }

    public ColoredMesh(Mesh mesh, List<Color> faceColors)
    {
        Mesh = mesh;
        FaceColors = faceColors;
    }

    public List<Color> GetVertexColors()
    {
        var colors = new List<Color>();

        for (int vertexIndex = 0; vertexIndex< Mesh.Vertices.Count; vertexIndex++)
        {
            var connectedFaceIndices = Mesh.Faces
                .Select((face,index) => (face, index))
                .Where(x => x.face.Vertices().Contains(vertexIndex))
                .Select(x => x.index)
                .ToList();

            var faceColors = connectedFaceIndices
                .Select(i => FaceColors[i])
                .ToList();

            var averageColor = faceColors.Average();
            colors.Add(averageColor);
        }

        return colors;
    }
}
