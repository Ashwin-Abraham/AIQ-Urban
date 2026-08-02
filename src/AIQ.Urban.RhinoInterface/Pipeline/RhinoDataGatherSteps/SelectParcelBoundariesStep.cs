using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class SelectParcelBoundariesStep(
    IEnumerable<RhinoObject> selectedObjects,
    ILoggerFactory? _loggerFactory = null) : IRhinoDataGatherStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<SelectParcelBoundariesStep>();

    public List<ICreationData> Run(RhinoDoc doc)
    {
        var selected = selectedObjects
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more parcel boundary curves first.");

        var result = new List<ICreationData>();
        var rejectedCount = 0;

        foreach (var obj in selected)
        {
            if (obj.Geometry is not Curve curve || !curve.TryNormalizeParcelBoundary(out var normalized))
            {
                rejectedCount++;
                continue;
            }

            result.Add(new ParcelBoundaryData(new Curve2D(normalized, obj.Id)));
            result.Add(new RhinoObjectDeletionData(obj.Id));
        }

        var boundaryCount = result.OfType<ParcelBoundaryData>().Count();
        if (boundaryCount == 0)
            throw new Exception("Selected parcel boundaries must be planar curves at Z = 0.");

        _logger.LogInformation(
            "SelectParcelBoundariesStep gathered {ValidCount} parcel boundaries and rejected {RejectedCount}",
            boundaryCount,
            rejectedCount);

        return result;
    }
}
