using ArchSitePlan.OSM.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;
using OsmSharp.Db;
using OsmSharp.Streams;

namespace ArchSitePlan.OSM.Models;

public class Highway (Polyline centerline, double? width, string highwayType): IOsmPolyline
{
    public Polyline Centerline { get; init; } = centerline;
    public double? Width { get; init; } = width;
    public string Key { get; init; } = Keys.Highway;
    public string Type { get; init; } = highwayType;

    public static IOsmPolyline? FromOsmWay(
        Way way, 
        List<Node> allNodes,
        List<string>? typeFilter = null)
    {
        string keyValue = string.Empty;
        bool exists = way.Tags?.TryGetValue(Keys.Highway, out keyValue)?? false;
        if (!exists) return null;

        if(typeFilter != null && !typeFilter.Contains(keyValue)) return null;

        var cl = Create.Polyline(way.ToBHoMPoints(allNodes));

        if (way.Tags!.TryGetValue(Keys.Width, out string value)
            && Double.TryParse(value, out var width))
            return new Highway(cl, width, keyValue);
        
        return new Highway(cl, null, keyValue);
    }
}
