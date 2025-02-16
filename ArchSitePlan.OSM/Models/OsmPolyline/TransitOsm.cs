using System.Drawing;
using ArchSitePlan.OSM.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;

namespace ArchSitePlan.OSM.Models.OsmPolyline;

public class TransitOsm(
    Polyline centerline, 
    double? width,
    string highwayType,
    string subType,
    long? id,
    Color color) : IOsmPolyline
{
    public Polyline Centerline { get; init; } = centerline;
    public double? Width { get; init; } = width;
    public string Type { get; init; } = highwayType;
    public string SubType { get; init; } = subType;
    public long? Id { get; init; } = id;
    public Color Color { get; init; } = color;

    public static IOsmPolyline? FromOsmWay(
        Way way,
        List<Node> allNodes,
        List<string>? typeFilter = null)
    {
        if (way.Tags == null) return null;

        string type = string.Empty;
        Color color = default;
        var acceptableKeys = Keys.TransitKeys;

        foreach(var key in acceptableKeys)
        {
            if (way.Tags.ContainsKey(key.Tag))
            {
                type = key.Tag;
                color = key.Color;

                if (key.Tag == Keys.Highway)
                {
                    color = Keys.RoadTypesAndColors.Find(x => x.Use == type).Color;
                }
                break;
            }
        }

        if (type == string.Empty || type == null || color == default) return null;

        
        string subtype = way.Tags[type];

        var cl = Create.Polyline(way.ToBHoMPoints(allNodes));

        if (way.Tags!.TryGetValue(Keys.Width, out string value)
            && double.TryParse(value, out var width))
            return new TransitOsm(cl, width, type, subtype, way.Id, color);

        return new TransitOsm(cl, null, type, subtype, way.Id, color);
    }
}
