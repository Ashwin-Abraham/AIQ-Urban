using AIQ.Urban.Core.Models.UrbanGraph;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;
public static class EdgeExtensions
{
    public static Line ToLine(this Edge edge)
    {
        return new Line(edge.Start.Point, edge.End.Point);
    }
}
