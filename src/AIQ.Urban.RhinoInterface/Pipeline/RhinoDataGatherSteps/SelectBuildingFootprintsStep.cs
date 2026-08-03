using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.Extensions;
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

public class SelectBuildingFootprintStep(
    IEnumerable<RhinoObject> selectedObjects,
    double floorHeight,
    int floorCount,
    Use use,
    ILoggerFactory? _loggerFactory = null) : IRhinoDataGatherStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SelectBuildingFootprintStep>();

    public List<IModelEditData> Run(RhinoDoc doc)
    {
        var selected = selectedObjects
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more building footprint curves first.");

        var result = new List<IModelEditData>();
        var rejectedCount = 0;
        foreach (var obj in selected)
        {
            if (obj.Geometry is not Curve curve)
                continue;

            if (curve.IsSelfIntersecting())
            {
                _logger.LogWarning(
                    "Skipped building footprint {ObjectId}: curve self-intersects within tolerance {Tolerance}",
                    obj.Id,
                    AIQ.Primitives.NumericTolerance.Default);
                rejectedCount++;
                continue;
            }

            var polygon = new Polygon2D(curve)
            {
                Id = obj.Id
            };
            result.Add(new BuildingFootprintData(polygon, floorHeight, floorCount, use));
            result.Add(new RhinoObjectDeletionData(obj.Id));
        }

        var buildingCount = result.OfType<BuildingFootprintData>().Count();
        _logger.LogInformation(
            "SelectBuildingFootprintStep gathered {ValidCount} building footprints and rejected {RejectedCount}",
            buildingCount,
            rejectedCount);

        return result;
    }
}
