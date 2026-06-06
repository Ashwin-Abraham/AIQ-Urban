using System;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.Urban.RhinoPlugin.MCP_Bridge;

public record CurveCoordinates(List<Point2d> Points)
{
    public List<(double X, double Y)> ToCoreTupleList() =>
        Points.Select(p => (p.X, p.Y)).ToList();
}
