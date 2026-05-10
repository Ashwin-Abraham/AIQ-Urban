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

    public static List<Curve> OffsetBothWays(this Curve centerline, double width, Plane plane, double tolerance)
    {
        var halfWidth = width / 2.0;
        var offsetCurves1 = centerline.Offset(plane, halfWidth, tolerance, CurveOffsetCornerStyle.Sharp);
        if (offsetCurves1 is not { Length: > 0 })
            throw new InvalidOperationException("Failed to offset centerline positively.");

        var offsetCurves2 = centerline.Offset(plane, -halfWidth, tolerance, CurveOffsetCornerStyle.Sharp);
        if (offsetCurves2 is not { Length: > 0 })
            throw new InvalidOperationException("Failed to offset centerline negatively.");

        var offset1 = offsetCurves1[0];
        var offset2 = offsetCurves2[0];

        if(centerline.IsClosed)
            return [offset1, offset2];

        var startCap = new Line(offset1.PointAtStart, offset2.PointAtStart).ToNurbsCurve();
        var endCap = new Line(offset2.PointAtEnd, offset1.PointAtEnd).ToNurbsCurve();

        var offset2Reversed = offset2.DuplicateCurve();
        offset2Reversed.Reverse();

        var segments = new Curve[] { offset1, endCap, offset2Reversed, startCap };
        var joined = Curve.JoinCurves(segments, tolerance);

        if (joined is not { Length: > 0 })
            throw new InvalidOperationException("Failed to join transit outline curves.");

        return [..joined];
    }
}
