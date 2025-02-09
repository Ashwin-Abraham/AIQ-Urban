using ArchSitePlan.OSM.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;

namespace ArchSitePlan.OSM.Models;

public class Building(
    Polygon boundary, 
    string type, 
    double? height,
    int? levels) : IOsmPolygon
{
    public Polygon Boundary { get; init; } = boundary;
    public string Key { get; init; } = Keys.Landuse;
    public string Type { get; init; } = type;
    public double? Height { get; init; } = height;
    public int? Levels { get; init; } = levels;

    public static IOsmPolygon? FromOsmWay(Way way, List<Node> allNodes)
    {
        string keyValue = string.Empty;
        bool exists = way.Tags?.TryGetValue(Keys.Building, out keyValue) ?? false;
        if (!exists) return null;

        var boundary = Create.Polygon(way.ToBHoMPoints(allNodes));

        double? height = null;
        if (way.Tags!.TryGetValue(Keys.Height, out string value)
           && Double.TryParse(value, out double heightFromTag))
            height = heightFromTag;

        int? levels = null;
        if (way.Tags!.TryGetValue(Keys.Levels, out string levelsString)
           && int.TryParse(levelsString, out int levelsFromTag))
            levels = levelsFromTag;

        return new Building(boundary, keyValue, height, levels);
    }
}
