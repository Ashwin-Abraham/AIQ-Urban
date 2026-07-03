using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;

public class Mesh2DConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Mesh2D || @object is IEnumerable<Mesh2D>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is Mesh2D mesh2d)
            AddToPreviewDoc(mesh2d, doc, color);

        if (@object is IEnumerable<Mesh2D> meshes)
            foreach (var m in meshes)
                AddToPreviewDoc(m, doc, color);
    }

    private static void AddToPreviewDoc(Mesh2D mesh2d, RhinoDoc doc, Color? color)
    {
        var mesh = mesh2d.Geometry;
        var attributes = RhinoHelper.MakeAttributes(
            doc, PreviewConstants.DebugCurveLayer, PreviewConstants.DebugGeometryColor, color);
        var edges = new HashSet<(int, int)>();
        foreach (var face in mesh.Faces)
        {
            if (face.IsTriangle)
            {
                AddEdge(mesh, doc, attributes, edges, face.A, face.B);
                AddEdge(mesh, doc, attributes, edges, face.B, face.C);
                AddEdge(mesh, doc, attributes, edges, face.C, face.A);
            }
            else if (face.IsQuad)
            {
                AddEdge(mesh, doc, attributes, edges, face.A, face.B);
                AddEdge(mesh, doc, attributes, edges, face.B, face.C);
                AddEdge(mesh, doc, attributes, edges, face.C, face.D);
                AddEdge(mesh, doc, attributes, edges, face.D, face.A);
            }
        }
    }

    private static void AddEdge(Mesh mesh, RhinoDoc doc, Rhino.DocObjects.ObjectAttributes attributes, HashSet<(int, int)> edges, int i, int j)
    {
        var key = i < j ? (i, j) : (j, i);
        if (!edges.Add(key))
            return;
        var line = new Line(mesh.Vertices[i], mesh.Vertices[j]);
        doc.Objects.AddLine(line, attributes);
    }
}
