using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;

public class FloorPlateConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is FloorPlate || @object is IEnumerable<FloorPlate>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is FloorPlate plate)
            AddToPreviewDoc(plate, doc, color);

        if (@object is IEnumerable<FloorPlate> plates)
            foreach (var p in plates)
                AddToPreviewDoc(p, doc, color);
    }

    private static void AddToPreviewDoc(FloorPlate plate, RhinoDoc doc, Color? color)
    {
        var layerPath = $"{PreviewConstants.BuildingFloorPlatesLayer}::{plate.Use.Name}";
        var attributes = RhinoHelper.MakeAttributes(doc, layerPath, plate.Use.Color, color);

        var breps = Brep.CreatePlanarBreps(plate.Outline.Geometry, 0.001);
        if (breps is { Length: > 0 })
        {
            var meshes = Mesh.CreateFromBrep(breps[0], MeshingParameters.FastRenderMesh);
            if (meshes != null)
                foreach (var m in meshes)
                    doc.Objects.AddMesh(m, attributes);
        }
    }
}
