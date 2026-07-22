using AIQ.Primitives.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace AIQ.Urban.Core.Extensions;
public static class Polygon2DExtensions
{
    public static Point3d Centroid(this Polygon2D polygon)
    {
        return AreaMassProperties.Compute(polygon.Geometry).Centroid;
    }

    public static IEnumerable<Line> Segments(this Polygon2D polygon)
    {
        for(int i = 0; i < polygon.Geometry.PointCount; i++)
        {
            var thisPoint = polygon.Geometry.Point(i);
            var nextPointIndex = (i + 1) % polygon.Geometry.PointCount;
            var nextPoint = polygon.Geometry.Point(nextPointIndex);

            yield return new Line(thisPoint, nextPoint);
        }
    }

    public static List<Polygon2D> Split(this Polygon2D polygon, IEnumerable<Curve2D> splitters, double tolerance = 1e-6)
    {

        var splitterList = splitters.ToList();

        bool highDegree = splitterList.Any(x => x.Geometry.Degree > 1);
        if (highDegree)
            return [polygon];

        var asPolycurve = PolyCurve2D.Create(polygon.Geometry);
        var splitPolyCurves = asPolycurve.Split(splitters, tolerance);

        var splitPolygons = splitPolyCurves
            .Select(x => Polygon2D.Create(x.Geometry))
            .ToList();

        return splitPolygons;
    }

    public static List<Point3d> Points(this Polygon2D polygon)
    {
        polygon.Geometry.TryGetPolyline(out var polyline);
        return [.. polyline];
    }


    public static bool ContainsOrCoincides(this Polygon2D polygon, Line line, double tolerance = 1e-5)
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

    public static PolyCurve2D ToPolyCurve2d(this Polygon2D polygon)
    {
        return new PolyCurve2D(polygon.Geometry);
    }
}
