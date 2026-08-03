using System;
using System.Collections.Generic;
using System.Linq;
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
        foreach (var obj in selected)
        {
            if (obj.Geometry is not Curve curve)
                continue;

            var polygon = new Polygon2D(curve)
            {
                Id = obj.Id
            };
            result.Add(new BuildingFootprintData(polygon, floorHeight, floorCount, use));
            result.Add(new RhinoObjectDeletionData(obj.Id));
        }

        var buildingCount = result.OfType<BuildingFootprintData>().Count();
        if (buildingCount == 0)
            throw new Exception("Selected objects must be curves.");

        _logger.LogInformation("SelectBuildingFootprintStep gathered {Count} building footprints", buildingCount);

        return result;
    }
}
