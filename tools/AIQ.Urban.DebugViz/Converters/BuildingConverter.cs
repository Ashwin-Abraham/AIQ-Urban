using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;

public class BuildingConverter : IGeometryPreviewConverter
{
    private readonly FloorPlateConverter _floorPlateConverter = new();

    public bool IsPreviewable(object @object) => @object is Building;

    public void AddToPreviewDoc(object @object, RhinoDoc doc)
    {
        if (@object is not Building building) return;

        var envLayer = RhinoHelper.EnsureLayer(
            doc, PreviewConstants.BuildingEnvelopeLayer, PreviewConstants.DebugGeometryColor);
        var envAttribs = new ObjectAttributes { LayerIndex = envLayer };

        var meshes = Mesh.CreateFromBrep(building.Envelope.Geometry, MeshingParameters.FastRenderMesh);
        if (meshes != null)
            foreach (var m in meshes)
                doc.Objects.AddMesh(m, envAttribs);

        foreach (var plate in building.FloorPlates)
            _floorPlateConverter.AddToPreviewDoc(plate, doc);
    }
}
