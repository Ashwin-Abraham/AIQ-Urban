using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class SelectZoneBoundaryStep(
    IEnumerable<RhinoObject> selectedObjects,
    string name,
    ILoggerFactory? _loggerFactory = null) : IRhinoDataGatherStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SelectZoneBoundaryStep>();

    public List<ICreationData> Run(RhinoDoc doc)
    {
        var selected = selectedObjects
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select a zone boundary curve first.");

        var obj = selected.First();
        var curve = TryValidateBoundaryCurve(obj.Geometry as Curve);
        if (curve is null)
            throw new Exception("Selected curve is not a valid closed planar boundary.");

        var boundary = new Polygon2D(curve);

        _logger.LogInformation("SelectZoneBoundaryStep gathered 1 zone boundary");

        return [new ZoneData(boundary, name)];
    }

    private static Curve? TryValidateBoundaryCurve(Curve? curve)
    {
        if (curve is null)
        {
            RhinoApp.WriteLine("Selected object is not a curve.");
            return null;
        }

        if (!curve.IsClosed)
        {
            RhinoApp.WriteLine("Zone boundary must be a closed curve.");
            return null;
        }

        if (!curve.IsPlanar())
        {
            RhinoApp.WriteLine("Non planar curves not allowed.");
            return null;
        }

        curve.TryGetPlane(out var plane);
        if (plane.Normal.Z < 0)
        {
            curve = curve.DuplicateCurve();
            curve.Reverse();
        }

        return curve;
    }
}
