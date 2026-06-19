using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;

public class BrepElementConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is BrepElement;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is not BrepElement brepElement) return;

        var meshes = Mesh.CreateFromBrep(brepElement.Geometry, MeshingParameters.FastRenderMesh);
        if (meshes == null) return;

        var attributes = RhinoHelper.MakeAttributes(
            doc, PreviewConstants.DebugBrepLayer, PreviewConstants.DebugGeometryColor, color);

        foreach (var m in meshes)
            doc.Objects.AddMesh(m, attributes);
    }
}
