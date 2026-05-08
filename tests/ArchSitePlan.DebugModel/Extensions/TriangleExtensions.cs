using Elements.Geometry;

namespace ArchSitePlan.DebugModel.Extensions;
public static class TriangleExtensions
{
    public static Color GetColor(this Triangle face)
    {
        var colors = face.Vertices.Select(x => x.Color).ToList();

        var averageColor = new Color(
            (int)colors.Average(c => c.Red),
            (int)colors.Average(c => c.Green),
            (int)colors.Average(c => c.Blue),
            (int)colors.Average(c => c.Alpha));

        return averageColor;
    }

    public static (Polygon AtXY, Transform ToGlobal) ToPolygonXY(this Triangle triangle)
    {
        var pts = triangle.Vertices.Select(v => v.Position).ToList();
        var normal = triangle.Normal;
        var xDir = (pts[1] - pts[0]).Unitized();
        var yDir = normal.Cross(xDir).Unitized();
        var origin = pts[0];
        var transform = new Transform(origin, xDir, yDir, normal);
        var globalPolygon = new Polygon(pts);

        var localPolygon = globalPolygon.TransformedPolygon(transform.Inverted());

        return (localPolygon, transform);
    }
}
