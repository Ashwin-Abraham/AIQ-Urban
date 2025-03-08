using AIQ.Primitives.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;

public static class PointExtensions
{
    public static bool AlmostEquals(this Point point, Point other, double tolerance = 1e-06)
    {
        return point.X.AlmostEquals(other.X, tolerance)
            && point.Y.AlmostEquals(other.Y, tolerance)
            && point.Z.AlmostEquals(other.Z, tolerance);
    }

    public static IEnumerable<Point> OrderAlong(this IEnumerable<Point> points, Line line)
    {
        return points.OrderBy(x => 
        {
            var projectedPoint = x.Project(line);
            return line.ParameterAtPoint(projectedPoint);
        });
    }
}
