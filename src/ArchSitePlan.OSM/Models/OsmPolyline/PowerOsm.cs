using System.Drawing;
using ArchSitePlan.OSM.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;

namespace ArchSitePlan.OSM.Models.OsmPolyline;

public class PowerOsm(
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

        var key = Keys.Power;
        if (way.Tags.TryGetValue(key, out string type))
            ;
        else
            return null;

        var cl = Create.Polyline(way.ToBHoMPoints(allNodes));

        return new PowerOsm(cl, type, way.Id, Colors.Power);
    }
}
