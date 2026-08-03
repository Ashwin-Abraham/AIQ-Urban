using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
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
    ILoggerFactory? _loggerFactory = null)
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SelectBuildingEnvelopeStep>();

    public List<ICreationData> Run(RhinoDoc doc)
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

        _logger.LogInformation("SelectBuildingEnvelopeStep gathered {Count} building envelopes", massData.Count);

        return massData.Cast<ICreationData>().ToList();
    }
}
