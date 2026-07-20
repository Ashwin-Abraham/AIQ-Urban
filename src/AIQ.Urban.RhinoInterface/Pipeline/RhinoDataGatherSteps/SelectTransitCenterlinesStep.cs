using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;
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

    public List<ICreationData> Run(RhinoDoc doc)
    {
        var selected = objectsToParse
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more transit centerline curves first.");

        var transitData = new List<TransitData>();

        foreach (var obj in selected)
        {
            if (obj.Geometry is not Curve curve)
                continue;

            var curve2D = new Curve2D(curve, obj.Id);
            transitData.Add(new TransitData(curve2D, width, transitType));
        }

        if (transitData.Count == 0)
            throw new Exception("Selected objects must be curves.");

        _logger.LogInformation("SelectTransitCenterlinesStep gathered {Count} transit centerlines", transitData.Count);

        return transitData.Cast<ICreationData>().ToList();
    }
}
