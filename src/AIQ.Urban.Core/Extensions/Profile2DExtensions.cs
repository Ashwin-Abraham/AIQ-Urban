using AIQ.Urban.Core.GeometryOperations;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;

public static class Profile2DExtensions
{
    public static Mesh Triangulate(this Profile2D profile, double tolerance = 1e-5)
    {
        var outer = profile.Outline.Geometry.PolylinePoints();
        var holes = profile.Holes
            .Select(h => h.Geometry.PolylinePoints())
            .ToList();

        return SweepLineTriangulator.Triangulate(outer, holes, tolerance);
    }

    private static IReadOnlyList<Point3d> PolylinePoints(this PolylineCurve curve)
    {
        var count = curve.PointCount;
        var points = new List<Point3d>(count);
        for (var i = 0; i < count; i++)
            points.Add(curve.Point(i));
        if (points.Count > 1 && points[0].DistanceTo(points[^1]) < 1e-6)
            points.RemoveAt(points.Count - 1);
        return points;
    }
}
