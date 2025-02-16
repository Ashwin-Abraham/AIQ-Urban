using System.Drawing;
using ArchSitePlan.OSM.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;

namespace ArchSitePlan.OSM.Models.OsmPolyline;

public class BoundaryBarrierOsm(
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

        var acceptableKeys = Keys.BoundaryBarrier;
        string? type = string.Empty;

        foreach (var key in acceptableKeys)
            if (way.Tags.TryGetValue(key, out type)) break;

        if (type == null || type == string.Empty) return null;

        var cl = Create.Polyline(way.ToBHoMPoints(allNodes));

        return new BoundaryBarrierOsm(cl, type, way.Id, Colors.BoundaryBarrier);
    }
}
