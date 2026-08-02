using AIQ.Primitives.Extensions;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;
public static class CurveExtensions
{
    public static IEnumerable<Point3d> Points(this Curve curve)
    {
        NurbsCurve nurbs = curve.ToNurbsCurve();

        for (int i = 0; i < nurbs.Points.Count; i++)
            yield return nurbs.Points[i].Location;
    }

    public static PolyCurve ToPolyCurve(this PolylineCurve curve)
    {
        var polyCurve = new PolyCurve();
        polyCurve.AppendSegment(curve);
        return polyCurve;
    }

    public static bool IsPlanarInXY(this Curve curve)
    {
        var xyPlane = new Plane(curve.PointAtStart, Vector3d.ZAxis);
        var bbox = curve.GetBoundingBox(xyPlane);

        return bbox.Min.Z.AlmostEquals(bbox.Max.Z);
    }

    public static List<Curve> OffsetBothWays(this Curve centerline, double width, Plane plane, double tolerance)
    {
        var halfWidth = width / 2.0;
        //return centerline.Offset(plane.Origin, plane.Normal, halfWidth, tolerance, 1e-5, false, CurveOffsetCornerStyle.None, CurveOffsetEndStyle.Flat)
        //    .ToList();

        var offsetCurves1 = centerline.Offset(plane.Origin, plane.Normal, halfWidth, tolerance, tolerance, false, CurveOffsetCornerStyle.Sharp, CurveOffsetEndStyle.None);

        if (offsetCurves1 is not { Length: > 0 })
            throw new InvalidOperationException("Failed to offset centerline positively.");

        var offsetCurves2 = centerline.Offset(plane.Origin, plane.Normal, -halfWidth, tolerance, tolerance, false, CurveOffsetCornerStyle.Sharp, CurveOffsetEndStyle.None);
        if (offsetCurves2 is not { Length: > 0 })
            throw new InvalidOperationException("Failed to offset centerline negatively.");

        var offset1Start = offsetCurves1[0];
        var offset1End = offsetCurves1[^1];

        var offset2Start = offsetCurves2[0];
        var offset2End = offsetCurves2[^1];

        if(centerline.IsClosed)
            return offsetCurves1.Concat(offsetCurves2).ToList();

        var startCap = new Line(offset1Start.PointAtStart, offset2Start.PointAtStart).ToNurbsCurve();
        var endCap = new Line(offset1End.PointAtEnd, offset2End.PointAtEnd).ToNurbsCurve();

        var offset2Reversed = offsetCurves2.Select(x =>x.DuplicateCurve()).ToList();
        offset2Reversed.ForEach(x => x.Reverse());

        var segments = offsetCurves1.Append(endCap).Concat(offset2Reversed).Append(startCap).ToArray();
        //new Curve[] { offset1, endCap, offset2Reversed, startCap };
        var joined = Curve.JoinCurves(segments, tolerance, true, simpleJoin: true, out _);

        if (joined is not { Length: > 0 })
            throw new InvalidOperationException("Failed to join transit outline curves.");

        return [..joined];
    }
}
