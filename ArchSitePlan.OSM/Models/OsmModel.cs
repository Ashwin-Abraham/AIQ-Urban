using ArchSitePlan.OSM.Extensions;
using OsmSharp;
using OsmSharp.Db;
using OsmSharp.Streams;

namespace ArchSitePlan.OSM.Models;

public class OsmModel
{
    public List<Highway> Highways { get; set; }
    public List<LandParcel> LandParcels { get; set; }
    public List<Building> Buildings { get; set; }

    public static OsmModel? FromOsmSource(XmlOsmStreamSource source)
    {
        var ways = source.Where(x => x is Way).Cast<Way>().ToList();
        var nodes = source.Where(x => x is Node).Cast<Node>().ToList();

        var model = new OsmModel();

        // Highways
        model.Highways = ways
            .Select(x => Highway.FromOsmWay(x, nodes) as Highway)
            .Where(x => x is not null)
            .Cast<Highway>()
            .ToList();

        // Landuse
        model.LandParcels = ways
            .Select(x => LandParcel.FromOsmWay(x, nodes) as LandParcel)
            .Where(x => x is not null)
            .Cast<LandParcel>()
            .ToList();

        model.Buildings = ways
            .Select(x => Building.FromOsmWay(x, nodes) as Building)
            .Where(x => x is not null)
            .Cast<Building>()
            .ToList();

        return model;
    }
}
