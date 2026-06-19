using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;
public static class Point2dExtensions
{
    public static Point3d ToPoint3d(this Point2d point, double z = 0)
    {
        return new Point3d(point.X, point.Y, z);
    }

    public static List<Point2d> GrahamConvexHull(this IEnumerable<Point2d> points)
    {
        var pointsArray = points.ToArray();
        var pivot = pointsArray[0];
        var pivotIdx = 0;
        for (var i = 1; i < pointsArray.Length; i++)
        {
            if (pointsArray[i].Y < pivot.Y - 1e-12 ||
                (Math.Abs(pointsArray[i].Y - pivot.Y) < 1e-12 && pointsArray[i].X < pivot.X - 1e-12))
            {
                pivot = pointsArray[i];
                pivotIdx = i;
            }
        }

        var sorted = pointsArray
            .Where((p, i) => i != pivotIdx)
            .OrderBy(p => Math.Atan2(p.Y - pivot.Y, p.X - pivot.X))
            .ThenBy(p => (p.X - pivot.X) * (p.X - pivot.X) + (p.Y - pivot.Y) * (p.Y - pivot.Y))
            .ToList();

        var hull = new List<Point2d> { pivot };

        foreach (var p in sorted)
        {
            while (hull.Count >= 2)
            {
                var a = hull[^2];
                var b = hull[^1];
                var cross = (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
                if (cross <= 1e-12)
                    hull.RemoveAt(hull.Count - 1);
                else
                    break;
            }
            hull.Add(p);
        }

        return hull;
    }
}
