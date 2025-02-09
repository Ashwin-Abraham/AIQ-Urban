using ArchSitePlan.OSM.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;
using OsmSharp.Db;
using OsmSharp.Streams;

namespace ArchSitePlan.OSM.Models;

public class LandParcel(Polygon boundary, string type): IOsmPolygon
{
    public Polygon Boundary { get; init; } = boundary;
    public string Key { get; init; } = Keys.Landuse;
    public string Type { get; init; } = type;

    public static IOsmPolygon? FromOsmWay(Way way, List<Node> allNodes)
    {
        string keyValue = string.Empty;
        bool exists = way.Tags?.TryGetValue(Keys.Landuse, out keyValue)?? false;
        if (!exists) return null;

        var wayIds = way.Nodes.ToList();

        var boundary = Create.Polygon(way.ToBHoMPoints(allNodes));

        return new LandParcel(boundary, keyValue);
    }
}
