using AIQ.Urban.Core.Models.UrbanGraph;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace AIQ.Urban.Core.Extensions;
public static class GraphNodeExtensions
{
    public static bool Intersects(this GraphNode node, Line line, double tolerance = 1e-5)
    {
        foreach(var edge in node.Edges)
        {
            var edgeLine = edge.ToLine();
            if (Intersection.LineLine(line, edgeLine, out _, out _, tolerance, true))
                return true;
        }

        return false;
    }
}
