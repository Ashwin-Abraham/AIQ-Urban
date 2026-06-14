using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;

public class BrepElementConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is BrepElement;

    public void AddToPreviewDoc(object @object, RhinoDoc doc)
    {
        if (@object is not BrepElement brepElement) return;

        var layer = RhinoHelper.EnsureLayer(
            doc, PreviewConstants.DebugBrepLayer, PreviewConstants.DebugGeometryColor);

        var meshes = Mesh.CreateFromBrep(brepElement.Geometry, MeshingParameters.FastRenderMesh);
        if (meshes != null)
        {
            var attribs = new ObjectAttributes { LayerIndex = layer };
            foreach (var m in meshes)
                doc.Objects.AddMesh(m, attribs);
        }
    }
}
