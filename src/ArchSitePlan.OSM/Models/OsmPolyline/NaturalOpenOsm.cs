using System.Drawing;
using ArchSitePlan.OSM.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;

namespace ArchSitePlan.OSM.Models.OsmPolyline;

public class NaturalOpenOsm(
    Polyline centerline, 
    string type, 
    long? id,
    Color color) : IOsmPolyline
{
    public Polyline Centerline { get; init; } = centerline;
    public string Type { get; init; } = type;
    public long? Id { get; init; } = id;
    public Color Color { get; init; } = color;

    public static IOsmPolyline? FromOsmWay(
        Way way,
        List<Node> allNodes,
        List<string>? typeFilter = null)
    {
        if (way.Tags == null) return null;

        Color color;
        string type = string.Empty;
        
        if (way.Tags.TryGetValue(Keys.Natural, out type))
            color = Colors.Natural;
        else
            return null;

        var cl = Create.Polyline(way.ToBHoMPoints(allNodes));

        return new NaturalOpenOsm(cl, type, way.Id, color);
    }
}
