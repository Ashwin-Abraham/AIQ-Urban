using System.Drawing;
using AIQ.PreviewHelper.Core;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;

public class DefaultBrepConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Brep || @object is IEnumerable<Brep>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is Brep brep)
            AddToPreviewDoc(brep, doc, color);

        if (@object is IEnumerable<Brep> breps)
            foreach (var b in breps)
                AddToPreviewDoc(b, doc, color);
    }

    private static void AddToPreviewDoc(Brep brep, RhinoDoc doc, Color? color)
    {
        var meshes = Mesh.CreateFromBrep(brep, MeshingParameters.FastRenderMesh);
        if (meshes == null) return;

        var attributes = RhinoHelper.MakeAttributes(
            doc, PreviewConstants.DebugBrepLayer, PreviewConstants.DebugGeometryColor, color);

        foreach (var m in meshes)
            doc.Objects.AddMesh(m, attributes);
    }
}
