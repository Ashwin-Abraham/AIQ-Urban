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
}
