using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class BuildingOutputter(ILoggerFactory? _loggerFactory = null) : IUrbanModelRhinoOutputter
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<BuildingOutputter>();

    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        var buildings = elements.OfType<Building>().ToList();
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

        _logger.LogInformation("Output {Count} buildings to Rhino", buildings.Count);
    }
}
