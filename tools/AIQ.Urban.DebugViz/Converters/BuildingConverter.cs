using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;

public class BuildingConverter : IGeometryPreviewConverter
{
    private readonly FloorPlateConverter _floorPlateConverter = new();

    public bool IsPreviewable(object @object) => @object is Building || @object is IEnumerable<Building>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is Building building)
            AddToPreviewDoc(building, doc, color);

        if (@object is IEnumerable<Building> buildings)
            foreach (var b in buildings)
                AddToPreviewDoc(b, doc, color);
    }

    private void AddToPreviewDoc(Building building, RhinoDoc doc, Color? color)
    {
        var envAttributes = RhinoHelper.MakeAttributes(
            doc, PreviewConstants.BuildingEnvelopeLayer, PreviewConstants.DebugGeometryColor, color);

        var meshes = Mesh.CreateFromBrep(building.Envelope.Geometry, MeshingParameters.FastRenderMesh);
        if (meshes != null)
            foreach (var m in meshes)
                doc.Objects.AddMesh(m, envAttributes);

        foreach (var plate in building.FloorPlates)
            _floorPlateConverter.AddToPreviewDoc(plate, doc);
    }
}
