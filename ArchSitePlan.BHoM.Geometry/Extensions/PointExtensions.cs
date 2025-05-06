using AIQ.Primitives.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;

public static class PointExtensions
{
    public static TransformMatrix ToTransform(this Point v)
    {
        double[,] matrix = new double[4, 4];

        // Identity matrix
        matrix[0, 0] = 1;
        matrix[1, 1] = 1;
        matrix[2, 2] = 1;
        matrix[3, 3] = 1;

        // Apply vector
        matrix[0, 3] = v.X;
        matrix[1, 3] = v.Y;
        matrix[2, 3] = v.Z;
        matrix[3, 3] = 1;

        var transform = new TransformMatrix() { Matrix = matrix };
        return transform;
    }
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
