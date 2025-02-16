using System.Drawing;
using ArchSitePlan.OSM.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;

namespace ArchSitePlan.OSM.Models.OsmPolygon;

public class NaturalClosedOsm(
    Polygon boundary, 
    string type, 
    long? id,
    Color color) : IOsmPolygon
{
    public Polygon Boundary { get; init; } = boundary;
    public string Type { get; init; } = type;
    public long? Id { get; init; } = id;
    public Color Color { get; init; } = color;

    public static IOsmPolygon? FromOsmWay(Way way, List<Node> allNodes)
    {
        var useAndColor = Keys.NaturalClosedTagsAndColors.FirstOrDefault(x => way.Tags?.TryGetValue(x.Tag, out _) ?? false);
        if (useAndColor == default) return null;

        var boundary = Create.Polygon(way.ToBHoMPoints(allNodes));

        return new NaturalClosedOsm(boundary, useAndColor.Tag, way.Id, useAndColor.Color);
    }
}