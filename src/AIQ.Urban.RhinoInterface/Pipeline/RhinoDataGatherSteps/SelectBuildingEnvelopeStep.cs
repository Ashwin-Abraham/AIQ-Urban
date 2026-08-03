using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface.Pipeline.EditData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class SelectBuildingEnvelopeStep(
    IEnumerable<RhinoObject> selectedObjects,
    double floorHeight,
    Use use,
    UrbanModel urbanModel,
    ILoggerFactory? _loggerFactory = null) : IRhinoDataGatherStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SelectBuildingEnvelopeStep>();

    public List<IModelEditData> Run(RhinoDoc doc)
    {
        var selected = selectedObjects
            .Where(obj => obj.ObjectType is ObjectType.Brep or ObjectType.Extrusion)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more building envelope Breps or Extrusions first.");

        var massData = new List<BuildingMassData>();

        foreach (var obj in selected)
        {
            Brep? brep = obj.Geometry switch
            {
                Brep b => b,
                Extrusion ext => ext.ToBrep(),
                _ => null
            };

            if (brep == null)
                continue;

            var envelope = new BrepElement(brep, obj.Id);
            massData.Add(new BuildingMassData(envelope, floorHeight, use));
        }

        if (massData.Count == 0)
            throw new Exception("Selected objects could not be converted to Brep envelopes.");

        var result = new BuildingEnvelopeDataGatherStep(
            massData,
            urbanModel,
            massData.Select(data => data.Envelope.Id).ToList()).Run(doc);

        _logger.LogInformation("SelectBuildingEnvelopeStep gathered {Count} building envelopes", massData.Count);

        return result;
    }
}
