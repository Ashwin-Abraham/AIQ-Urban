using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class BuildingOutputter(List<Building> buildings) : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc)
    {
        var envelopeAttributes = RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.BuildingEnvelope, ColorUtils.BuildingEnvelopeColor);

        foreach (var building in buildings)
        {
            var newEnvelopeId = doc.Objects.AddBrep(building.Envelope.Geometry, envelopeAttributes);
            building.Envelope.Id = newEnvelopeId;

            foreach (var plate in building.FloorPlates)
            {
                var plateAttributes = RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.BuildingFloorPlate(plate.Use), plate.Use.Color);
                var newId = doc.Objects.AddCurve(plate.Outline.Geometry, plateAttributes);
                plate.Outline.Id = newId;
            }
        }
    }
}
