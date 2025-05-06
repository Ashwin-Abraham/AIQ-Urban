using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;

public static class PolygonExtensions
{
    public static Polyline ToClosedPolyline(this Polygon polygon)
    {
        var vertices = polygon.Vertices.ToList();
        vertices.Add(polygon.Vertices[0]);

        return Create.Polyline(vertices);
    }

    public static Polygon Transform(this Polygon polygon, TransformMatrix transform)
    {
        var vertices = polygon.Vertices.Select(x => x.Transform(transform)).ToList();
        return  new Polygon(vertices);
    }
}
