using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;
using OsmSharp.Db;

namespace ArchSitePlan.OSM.Models;

public class Highway (Polyline centerline, double width, string highwayType): IOsmPolyline
{
    public Polyline Centerline { get; init; } = centerline;
    public double Width { get; init; } = width;
    public string Key { get; init; } = Keys.Highway;
    public string Type { get; init; } = highwayType;

    public static IOsmPolyline FromOsm(Way way, IOsmGeoSource source)
    {
        var points = way.Nodes
            .Select(node => source.GetNode(node))
            .Select(x => Create.Point(x.Latitude ?? 0, x.Longitude ?? 0))
            .ToList();

        var cl = Create.Polyline(points);

        bool exists = way.Tags.TryGetValue(Keys.Highway, out var keyValue);

        if (!exists) return null;

        return new Highway(cl, 12, keyValue);
    }
}
