using BH.Engine.Geometry;
using BH.oM.Geometry;
using OsmSharp;
using OsmSharp.Db;

namespace ArchSitePlan.OSM.Models;

public class OsmHighWay(Polyline centerline, double width, string keyValue)
{
    public Polyline Centerline { get; private set; } = centerline;
    public double Width { get; private set; } = width;
    public static string Key { get;} = Keys.Highway;
    public string KeyValue { get;} = keyValue;

    public static OsmHighWay? FromWay(Way way, IOsmGeoSource source)
    {
        var points = way.Nodes
            .Select(node => source.GetNode(node))
            .Select(x => Create.Point(x.Latitude?? 0, x.Longitude?? 0))
            .ToList();

        var cl = Create.Polyline(points);

        bool exists = way.Tags.TryGetValue(OsmHighWay.Key, out var keyValue);

        OsmHighWay? highway = null;

        if (!exists) return null;

        return new OsmHighWay(cl, 12, keyValue);
    }
}
