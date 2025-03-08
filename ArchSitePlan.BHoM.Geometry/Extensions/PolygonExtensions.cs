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
}
