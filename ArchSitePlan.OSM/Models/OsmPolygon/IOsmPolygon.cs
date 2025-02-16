using BH.oM.Geometry;
using OsmSharp;
using OsmSharp.Db;

namespace ArchSitePlan.OSM.Models.OsmPolygon;

public interface IOsmPolygon : IOsmObject
{
    public Polygon Boundary { get; init; }
    public string Type { get; init; }
    public static abstract IOsmPolygon? FromOsmWay(Way way, List<Node> allNodes);
}
