using BH.oM.Geometry;
using OsmSharp;
using OsmSharp.Db;

namespace ArchSitePlan.OSM.Models.OsmPolyline;

public interface IOsmPolyline : IOsmObject
{
    public Polyline Centerline { get; init; }
    public string Type { get; init; }
    public long? Id { get; init; }
    public static abstract IOsmPolyline? FromOsmWay(
        Way way,
        List<Node> allNodes,
        List<string>? typeFilter = null);
}
