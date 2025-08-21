using AIQ.Primitives.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;

public static class PointExtensions
{
    //
    // Summary:
    //     Calculate a polygon from the 2d convex hull of a collection of points. Adapted
    //     from https://rosettacode.org/wiki/Convex_hull#C.23 and the Hypar Elements library
    //
    // Parameters:
    //   points:
    //     A collection of points
    //
    // Returns:
    //     A polygon representing the convex hull of the provided points.
    public static Polygon? ConvexHullFromPoints(this IEnumerable<Point> points)
    {
        if (!points.Any())
        {
            return null;
        }

        Point[] array = (from p in points
                           select Create.Point(p.X, p.Y) into p
                           orderby p.X, p.Y
                           select p).ToArray();

        List<Point> list = new List<Point>();
        Func<Point, Point, Point, bool> func = (Point a, Point b, Point c) => (b.X - a.X) * (c.Y - a.Y) > (b.Y - a.Y) * (c.X - a.X);
        Point[] array2 = array;
        foreach (Point vector in array2)
        {
            while (list.Count >= 2 && !func(list[list.Count - 2], list[list.Count - 1], vector))
            {
                list.RemoveAt(list.Count - 1);
            }

            list.Add(vector);
        }

        int num = list.Count + 1;
        for (int num2 = array.Length - 1; num2 >= 0; num2--)
        {
            Point vector2 = array[num2];
            while (list.Count >= num && !func(list[list.Count - 2], list[list.Count - 1], vector2))
            {
                list.RemoveAt(list.Count - 1);
            }

            list.Add(vector2);
        }

        list.RemoveAt(list.Count - 1);
        return Create.Polygon(list);
    }

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

    public static Vector ToVector(this Point point)
    {
        return Create.Vector(point.X, point.Y, point.Z);
    }

    public static bool AreColinear2D(this IEnumerable<Point> points)
    {
        var pointList = points.ToList();
        if (pointList.Count < 3)
            return true; 
        
        for (int i = 0; i< pointList.Count - 2; i++)
        {
            var point1 = pointList[i];
            var point2 = pointList[i+1];
            var point3 = pointList[i+2];

            bool colinear = ((point2.X - point1.X)* (point3.Y - point1.Y))
                .AlmostEquals((point2.Y - point1.Y) * (point3.X - point1.X));

            if(!colinear)
                return false;
        }

        return true;
    }
}
