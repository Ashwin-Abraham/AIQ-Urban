using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;
public static class Polygon2DExtensions
{
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


    public static List<Point3d> Points(this Polygon2D polygon)
    {
        polygon.Geometry.TryGetPolyline(out var polyline);
        return [.. polyline];
    }
}
