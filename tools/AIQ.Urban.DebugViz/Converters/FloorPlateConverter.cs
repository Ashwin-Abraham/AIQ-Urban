using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;

public class FloorPlateConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is FloorPlate;

    public void AddToPreviewDoc(object @object, RhinoDoc doc)
    {
        if (@object is not FloorPlate plate) return;

        var layer = RhinoHelper.EnsureLayer(
            doc, $"{PreviewConstants.BuildingFloorPlatesLayer}::{plate.Use.Name}", plate.Use.Color);
        var attribs = new ObjectAttributes { LayerIndex = layer };

        var breps = Brep.CreatePlanarBreps(plate.Outline.Geometry, 0.001);
        if (breps is { Length: > 0 })
        {
            var meshes = Mesh.CreateFromBrep(breps[0], MeshingParameters.FastRenderMesh);
            if (meshes != null)
                foreach (var m in meshes)
                    doc.Objects.AddMesh(m, attribs);
        }
    }
}
