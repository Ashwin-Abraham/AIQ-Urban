using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;
public static class Point3dExtensions
{
    public static Point2d ToPoint2d(this Point3d point)
    {
        return new Point2d(point.X, point.Y);
    }

    public static Vector3d ToVector3d(this Point3d point)
    {
        return new Vector3d(point.X, point.Y, point.Z);
    }

    internal static double CrossZ(this Point3d p, Point3d q, Point3d r)
    {
        return (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
    }

    internal static bool IsInsideCircumcircle(this Point3d d, Point3d a, Point3d b, Point3d c, double tolerance)
    {
        if (a.CrossZ(b, c) < 0)
            (b, c) = (c, b);

        var a2 = a.X * a.X + a.Y * a.Y;
        var b2 = b.X * b.X + b.Y * b.Y;
        var c2 = c.X * c.X + c.Y * c.Y;
        var d2 = d.X * d.X + d.Y * d.Y;

        var det = (a.X * (b.Y * c2 - b2 * c.Y) - a.Y * (b.X * c2 - b2 * c.X) + a2 * (b.X * c.Y - b.Y * c.X))
                - (a.X * (b.Y * d2 - b2 * d.Y) - a.Y * (b.X * d2 - b2 * d.X) + a2 * (b.X * d.Y - b.Y * d.X))
                + (a.X * (c.Y * d2 - c2 * d.Y) - a.Y * (c.X * d2 - c2 * d.X) + a2 * (c.X * d.Y - c.Y * d.X))
                - (b.X * (c.Y * d2 - c2 * d.Y) - b.Y * (c.X * d2 - c2 * d.X) + b2 * (c.X * d.Y - c.Y * d.X));

        return det > tolerance;
    }
}
