using System.Drawing;
using ArchSitePlan.OSM.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;

namespace ArchSitePlan.OSM.Models.OsmPolygon;

public class BuildingOsm(
    Polygon boundary,
    string type,
    double? height,
    int? levels,
    long? id,
    Color color) : IOsmPolygon
{
    public Polygon Boundary { get; init; } = boundary;
    public string Key { get; init; } = Keys.Landuse;
    public string Type { get; init; } = type;
    public double? Height { get; init; } = height;
    public int? Levels { get; init; } = levels;
    public long? Id { get; init; } = id;
    public Color Color { get; init; } = color;

    public static IOsmPolygon? FromOsmWay(Way way, List<Node> allNodes)
    {
        string keyValue = string.Empty;
        bool isBuilding = way.Tags?.TryGetValue(Keys.Building, out keyValue) ?? false;
        if (!isBuilding) return null;

        // Sometimes other tags represent a buildings use better
        foreach (var tag in Keys.SupercedingBuildingUseTags)
        {
            string newUse = "";
            bool found = way.Tags?.TryGetValue(tag, out newUse) ?? false;
            if (found)
            {
                keyValue = newUse;
                break;
            }
        }

        var boundary = Create.Polygon(way.ToBHoMPoints(allNodes));

        double? height = null;
        if (way.Tags!.TryGetValue(Keys.Height, out string value)
           && double.TryParse(value, out double heightFromTag))
            height = heightFromTag;

        int? levels = null;
        if (way.Tags!.TryGetValue(Keys.Levels, out string levelsString)
           && int.TryParse(levelsString, out int levelsFromTag))
            levels = levelsFromTag;

        return new BuildingOsm(boundary, keyValue, height, levels, way.Id, Colors.Building);
    }
}
