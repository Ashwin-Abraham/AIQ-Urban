using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;
public static class BoundingBoxExtensions
{
    public static double Length(this BoundingBox box, Axis axis)
    {
        switch (axis)
        {
            case Axis.X:
                return Math.Abs(box.Max.X - box.Min.X);
            case Axis.Y:
                return Math.Abs(box.Max.Y - box.Min.Y);
            case Axis.Z:
            default:
                return Math.Abs(box.Max.Z - box.Min.Z);
        }
    }

    public static double AreaXY(this BoundingBox box)
    {
        return box.Length(Axis.X) * box.Length(Axis.Y);
    }

    public static Polyline ToPolylineXY(this BoundingBox box)
    {
        var points = box.GetCorners();
        
        var plinePoints = points.Take(4).Append(points[0]);

        return new Polyline(plinePoints);
    }
}
