using BH.oM.Geometry;
using OsmSharp;
using OsmSharp.Db;

namespace ArchSitePlan.OSM.Models;

public interface IOsmObject { }

public interface IOsmPolyline : IOsmObject
{
    public Polyline Centerline { get; init; }
    public double Width { get; init; }
    public string Type { get; init; }
    public string Key {get; init}
    public static abstract IOsmPolyline FromOsm(Way way, IOsmGeoSource source);
}

