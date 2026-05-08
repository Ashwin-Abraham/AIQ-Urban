using System.Drawing;
using ArchSitePlan.OSM.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;
using OsmSharp.Db;
using OsmSharp.Streams;

namespace ArchSitePlan.OSM.Models.OsmPolygon;

public class LandParcelOsm(
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
        if (way.Tags == null) return null;

        var primaryLanduseTags = Keys.SupercedingLanduseTagsAndColors;
        string type;
        Color color;

        // Prefer to get the landuse from the superceding tag first
        var keyAndColor = primaryLanduseTags.FirstOrDefault(x => way.Tags.ContainsKey(x.Tag));

        if (keyAndColor != default)
        {
            type = keyAndColor.Tag;
            color = keyAndColor.Color;
        }

        // If not that then use the landuse tag
        else if (way.Tags.TryGetValue(Keys.Landuse, out string value))
        {
            type = value;
            color = Colors.DefaultLanduse;
        }

        // If none of these exist on the way, it is not a land parcel
        else
            return null;

        // Special case: if landuse is amenity get the type of amenity
        if (type.Equals(Keys.Amenity))
            type = way.Tags[Keys.Amenity];

        var boundary = Create.Polygon(way.ToBHoMPoints(allNodes));
        
        if (boundary == null) return null;

        return new LandParcelOsm(boundary, type, way.Id, color);
    }
}
