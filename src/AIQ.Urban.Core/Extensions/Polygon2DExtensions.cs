using AIQ.Primitives.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace AIQ.Urban.Core.Extensions;
public static class Polygon2DExtensions
{
    public static Point3d Centroid(this Polycurve2D polygon)
    {
        return AreaMassProperties.Compute(polygon.Geometry).Centroid;
    }

    public static IEnumerable<Line> Segments(this Polycurve2D polygon)
    {
        for(int i = 0; i < polygon.Geometry.PointCount; i++)
        {
            var thisPoint = polygon.Geometry.Point(i);
            var nextPointIndex = (i + 1) % polygon.Geometry.PointCount;
            var nextPoint = polygon.Geometry.Point(nextPointIndex);

            yield return new Line(thisPoint, nextPoint);
        }
    }

    public static List<Polycurve2D> Split(this Polycurve2D polygon, IEnumerable<Curve2D> splitters, double tolerance = 1e-6)
    {
        var smallOffset = tolerance * 10;

        List<Curve> splitterCrvs = [];

        foreach(var splitter in splitters)
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
            .Select(x => Polycurve2D.Create(x))
            .OfType<Polycurve2D>()
            .ToList();

        if(result.Count == 0)
            return [polygon];

        return result;
    }

    public static List<Point3d> Points(this Polycurve2D polygon)
    {
        polygon.Geometry.TryGetPolyline(out var polyline);
        return [.. polyline];
    }


    public static bool ContainsOrCoincides(this Polycurve2D polygon, Line line, double tolerance = 1e-5)
    {
        var intersections = Intersection.CurveCurve(polygon.Geometry, line.ToNurbsCurve(), tolerance, tolerance);
        var lineStart = line.PointAt(0);
        var lineEnd = line.PointAt(1);

        foreach(var intersection in intersections)
        {
            if (intersection.IsOverlap)
                continue;

            var linePoint = intersection.PointB;
            if (linePoint.DistanceTo(lineStart).GreaterThan(0) && linePoint.DistanceTo(lineEnd).GreaterThan(0))
                return false;
        }

        var containment = polygon.Geometry.Contains(line.PointAt(0.5), Plane.WorldXY, tolerance);
        return containment == PointContainment.Inside || containment == PointContainment.Coincident;
    }
}
