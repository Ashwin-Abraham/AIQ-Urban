using AIQ.Primitives.Extensions;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;
public static class CurveExtensions
{
    public static bool IsPlanarInXY(this Curve curve)
    {
        var xyPlane = new Plane(curve.PointAtStart, Vector3d.ZAxis);
        var bbox = curve.GetBoundingBox(xyPlane);

        return bbox.Min.Z.AlmostEquals(bbox.Max.Z);
    }
}
