using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;
public static class PolyCurve2DExtensions
{

    public static bool ContainsOrCoincides(this PolyCurve2D curve, Line line, double tolerence = 1e-5)
    {
        if(!curve.Geometry.IsClosed)
            return false;

        var startContainment = curve.Geometry.Contains(line.From, Plane.WorldXY, tolerence);

        if (startContainment == PointContainment.Unset || startContainment == PointContainment.Outside)
            return false;

        var endContainment = curve.Geometry.Contains(line.From, Plane.WorldXY, tolerence);
        if (endContainment == PointContainment.Unset || endContainment == PointContainment.Outside)
            return false;

        return true;
    }

    public static IEnumerable<Point3d> Points(this PolyCurve2D curve)
    {
        return curve.Geometry.Points();
    }

    public static List<PolyCurve2D> Split(this PolyCurve2D polygon, IEnumerable<Curve2D> splitters, double tolerance = 1e-6)
    {
        var smallOffset = tolerance * 10;

        List<Curve> splitterCrvs = [];

        foreach (var splitter in splitters)
        {
            List<Curve> asCurves;

            if (splitter.Geometry.IsClosed)
                asCurves = splitter.Geometry.Split(0.5).ToList();
            else
                asCurves = [splitter.Geometry];

            splitterCrvs.AddRange(asCurves);
        }

        if (splitterCrvs.Count == 0)
            return [polygon];

        var splitterExpanded = splitterCrvs.SelectMany(x => x.OffsetBothWays(smallOffset, Plane.WorldXY, tolerance));
        var polygonShrunk = polygon.Geometry.Offset(Plane.WorldXY, smallOffset, tolerance, CurveOffsetCornerStyle.Sharp).FirstOrDefault();

        if (polygonShrunk == null)
            return [polygon];

        var splitPolygon = Curve.CreateBooleanDifference(polygonShrunk, splitterExpanded, tolerance).ToList();

        // If the splitters didnt go all the way through, or didnt even touch the polygon, then you wont have more than 1 result
        if (splitPolygon.Count <= 1)
            return [polygon];

        splitPolygon = splitPolygon.SelectMany(x => x.Offset(Plane.WorldXY, smallOffset, tolerance, CurveOffsetCornerStyle.Sharp)).ToList();

        var result = splitPolygon
            .Select(x => PolyCurve2D.Create(x))
            .OfType<PolyCurve2D>()
            .ToList();

        if (result.Count == 0)
            return [polygon];

        return result;
    }
}
