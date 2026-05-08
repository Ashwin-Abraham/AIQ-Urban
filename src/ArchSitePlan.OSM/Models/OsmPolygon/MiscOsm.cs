using System.Drawing;
using ArchSitePlan.OSM.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;

namespace ArchSitePlan.OSM.Models.OsmPolygon;

public class MiscOsm(Polygon boundary, string type, long? id, Color color) : IOsmPolygon
{
    public Polygon Boundary { get; init; } = boundary;
    public string Key { get; init; } = Keys.Place;
    public string Type { get; init; } = type;
    public long? Id { get; init; } = id;
    public Color Color { get; init; } = color;

    public static IOsmPolygon? FromOsmWay(Way way, List<Node> allNodes)
    {
        string keyValue = string.Empty;
        bool exists = way.Tags?.TryGetValue(Keys.Place, out keyValue) ?? false;
        if (!exists) return null;

        var boundary = Create.Polygon(way.ToBHoMPoints(allNodes));

        return new MiscOsm(boundary, keyValue, way.Id, Colors.Misc);
    }
}
