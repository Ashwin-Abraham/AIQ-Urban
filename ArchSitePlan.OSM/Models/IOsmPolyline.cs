using BH.oM.Geometry;
using OsmSharp;
using OsmSharp.Db;

namespace ArchSitePlan.OSM.Models;

public interface IOsmPolyline : IOsmObject
{
    public Polyline Centerline { get; init; }
    public double? Width { get; init; }
    public string Type { get; init; }
    public string Key { get; init; }
    public static abstract IOsmPolyline? FromOsmWay(
        Way way, 
        List<Node> allNodes, 
        List<string>? typeFilter = null);
}
