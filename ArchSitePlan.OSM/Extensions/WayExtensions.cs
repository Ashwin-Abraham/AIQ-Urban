using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;

namespace ArchSitePlan.OSM.Extensions;

public static class WayExtensions
{
    public static bool HasLanduse(this OsmGeo geo)
    {
        string use = string.Empty;
        bool exists = geo.Tags?.TryGetValue(Keys.Landuse, out use)?? false;
        return exists && use != string.Empty;
    }
    public static List<Point> ToBHoMPoints(this Way way, List<Node> nodes)
    {
        var wayIds = way.Nodes.ToList();

        var filteredNodeLookup =
            nodes
            .Where(node => wayIds.Contains(node.Id ?? 0))
            .ToLookup(node => node.Id);

        var orderedNodes = way.Nodes
            .SelectMany(x => filteredNodeLookup[x])
            .ToList();

        var points = orderedNodes
            .Select(x => ToMeterPoint(x))
            .ToList();

        return points;
    }

    private static Point? ToMeterPoint(Node node)
    {
        if (!node.Latitude.HasValue || !node.Longitude.HasValue) return null;

        double x = 111.32 * 1000 * node.Latitude.Value;

        double lengthPerLongt = 40075 * 1000 * Math.Cos(node.Latitude.Value) / 360;
        double y = node.Longitude.Value * lengthPerLongt;

        return Create.Point(x, y);
    }
}
