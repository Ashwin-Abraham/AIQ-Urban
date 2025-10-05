using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.DebugModel.Extensions;
public static class PolygonExtensions
{
    public static Polygon EnsureClockwise(this Polygon polygon)
    {
        if(polygon.IsClockWise())
            return polygon;
        else
            return polygon.Reversed();
    }

    public static Polygon ReversedWithSameStart(this Polygon polygon)
    {
        var vertices = polygon.Vertices.Reverse().ToList();
        vertices = vertices.Prepend(vertices.Last()).ToList();
        vertices.RemoveAt(vertices.Count - 1);

        return new Polygon(vertices);
    }
}
