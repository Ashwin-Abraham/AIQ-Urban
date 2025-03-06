using AIQ.Primitives.Extensions;
using ArchSitePlan.BHoM.Geometry;
using ArchSitePlan.BHoM.Geometry.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;

public static class PolyCurveExtensions
{
    private sealed record MinBox(double Area, double Rotation, BoundingBox Bounds, PolyCurve RotatedCrv);
    
    public static PolyCurve ToPolyCurve(this ICurve curve)
    => Create.PolyCurve([curve]);
    
    public static Rectangle GetMinimumBoundingBox(this PolyCurve curve)
    {
        var minBox = new MinBox(double.MaxValue, 0, new(), new());

        for (double rotation = 0; rotation < 360 ; rotation++)
        {
            var rotated = curve.Rotate(
                Point.Origin, 
                Vector.ZAxis, 
                rotation.ToRadians());

            var bounds = rotated.Bounds();
            var area = (bounds.Max.X - bounds.Min.X) * (bounds.Max.Y - bounds.Min.Y);

            if(minBox.Area > area)
                minBox = new MinBox(area, rotation, bounds, rotated);
        }

        var minRectangle = new Rectangle(minBox.Bounds.Min, minBox.Bounds.Max);
        var revertedMinBox =  minRectangle.Rotate(Point.Origin,
            Vector.ZAxis, 
            -minBox.Rotation.ToRadians());

        return revertedMinBox!;
    }

    public static bool ValidateContinuity(this PolyCurve curve)
    {
        var segments = curve.Curves;

        //if (segments.Exists(s => s is PolyCurve))
        //    return false;

        var toTest = segments.Where(s => s is not Polygon).ToList();

        for (int i = 0; i < toTest.Count - 1; i++)
        {
            var currentCrvEnd = toTest[i].IEndPoint();
            var nextCrvStart = toTest[i + 1].IStartPoint();

            if (!currentCrvEnd.IsEqual(nextCrvStart))
                return false;
        }

        return true;
    }

    public static PolyCurve? OffsetSafe(this PolyCurve curve, double distance, Vector normal)
    {
        if (curve.Curves.Count == 1 && curve.Curves[0].IIsLinear())
        {
            var firstCurve = curve.Curves[0];
            Line line = Create.Line(firstCurve.IStartPoint(), firstCurve.IEndPoint());
            Line offsetLine = line.Offset(distance, normal);
            var result = Create.PolyCurve([offsetLine]);

            return result;
        }
        else if(curve.Curves.Count == 1 && curve.Curves[0] is Polygon)
        {
            var offset = (curve.Curves[0] as Polygon).IOffset(distance, normal);
            return Create.PolyCurve([offset]);
        }
        else
        {
            return curve.Offset(distance, normal);
        }
    }

    public static List<PolyCurve> OffsetBothSides(this PolyCurve curve, double distance, Vector normal)
    {
        PolyCurve? curve1 = curve.OffsetSafe(distance, normal);
        PolyCurve? curve2 = curve.OffsetSafe(-distance, normal)?.Flip();
        
        if (curve.IsClosed())
            return new List<PolyCurve>() { curve1, curve2 };

        Line cap1 = Create.Line(curve1.EndPoint(), curve2.StartPoint());
        Line cap2 = Create.Line(curve2.EndPoint(), curve1.StartPoint());

        PolyCurve joinedCrv = new PolyCurve();
        joinedCrv.Curves.AddRange(curve1.Curves);
        joinedCrv.Curves.Add(cap1);
        joinedCrv.Curves.AddRange(curve2.Curves);
        joinedCrv.Curves.Add(cap2);

        return [joinedCrv];
    }

    public static bool IsContainingAny(this PolyCurve curve, List<Point> points, bool acceptOnEdge = true, double tolerance = 1E-06)
    {
        foreach (Point point in points)
        {
            if (curve.IsContaining([point], acceptOnEdge, tolerance))
                return true;
        }

        return false;
    }
}
