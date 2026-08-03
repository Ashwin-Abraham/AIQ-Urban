using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface.Pipeline.EditData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class SelectTransitCenterlinesStep(
    IEnumerable<RhinoObject> objectsToParse,
    double width,
    TransitType transitType,
    ILoggerFactory? _loggerFactory = null) : IRhinoDataGatherStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SelectTransitCenterlinesStep>();

    public List<IModelEditData> Run(RhinoDoc doc)
    {
        var selected = objectsToParse
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .DistinctBy(obj => obj.Id)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more transit centerline curves first.");

        var result = new List<IModelEditData>();
        var acceptedCenterlineIds = new List<Guid>();
        var rejectedCount = 0;

        foreach (var obj in selected)
        {
            if (obj.Geometry is not Curve curve)
                continue;

            if (!curve.TryNormalizeTransitCenterline(out var normalizedCurve))
            {
                var bounds = curve.GetBoundingBox(true);
                _logger.LogWarning(
                    "Skipped transit centerline {ObjectId}: Z range [{MinZ}, {MaxZ}] must lie at 0 within tolerance {Tolerance}",
                    obj.Id,
                    bounds.Min.Z,
                    bounds.Max.Z,
                    AIQ.Primitives.NumericTolerance.Default);
                rejectedCount++;
                continue;
            }

            if (normalizedCurve.IsSelfIntersecting())
            {
                _logger.LogWarning(
                    "Skipped transit centerline {ObjectId}: curve self-intersects within tolerance {Tolerance}",
                    obj.Id,
                    AIQ.Primitives.NumericTolerance.Default);
                rejectedCount++;
                continue;
            }

            var curve2D = new Curve2D(normalizedCurve, obj.Id);
            result.Add(new TransitData(curve2D, width, transitType));
            result.Add(new RhinoObjectDeletionData(obj.Id));
            acceptedCenterlineIds.Add(obj.Id);
        }

        if (acceptedCenterlineIds.Count > 0)
            result.Add(new TransitDeletionData(acceptedCenterlineIds.Distinct().ToList()));

        var transitCount = result.OfType<TransitData>().Count();
        _logger.LogInformation(
            "SelectTransitCenterlinesStep gathered {ValidCount} transit centerlines and rejected {RejectedCount}",
            transitCount,
            rejectedCount);

        return result;
    }
}
