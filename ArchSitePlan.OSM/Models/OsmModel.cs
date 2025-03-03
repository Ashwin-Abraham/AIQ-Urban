using ArchSitePlan.OSM.Extensions;
using ArchSitePlan.OSM.Models.OsmPolygon;
using ArchSitePlan.OSM.Models.OsmPolyline;
using OsmSharp;
using OsmSharp.Streams;

namespace ArchSitePlan.OSM.Models;

public class OsmModel(
    List<TransitOsm> highways,
    List<LandParcelOsm> landParcels,
    List<BuildingOsm> buildings,
    List<NaturalClosedOsm> naturalAreas,
    List<MiscOsm> miscAreas,
    List<NaturalOpenOsm> naturalOpen,
    List<WaterwayOsm> waterways,
    List<BoundaryBarrierOsm> boundaryBarrier,
    List<PowerOsm> power)
{
    public List<TransitOsm> Highways { get; set; } = highways;
    public List<LandParcelOsm> LandParcels { get; set; } = landParcels;
    public List<BuildingOsm> Buildings { get; set; } = buildings;
    public List<NaturalClosedOsm> NaturalAreas { get; set; } = naturalAreas;
    public List<MiscOsm> MiscAreas { get; set; } = miscAreas;
    public List<NaturalOpenOsm> NaturalOpen { get; set; } = naturalOpen;
    public List<BoundaryBarrierOsm> BoundaryBarrier { get; set; } = boundaryBarrier;
    public List<PowerOsm> Power { get; set; } = power;
    public List<WaterwayOsm> Waterways { get; set; } = waterways;

    public List<IOsmObject> GetModelObjects()
    {
        return Highways.Cast<IOsmObject>()
            .Concat(LandParcels)
            .Concat(Buildings)
            .Concat(NaturalAreas)
            .Concat(MiscAreas)
            .Concat(NaturalOpen)
            .Concat(BoundaryBarrier)
            .Concat(Power)
            .Concat(Waterways)
            .ToList();
    }

    public static OsmModel? FromOsmSource(XmlOsmStreamSource source)
    {
        var ways = source.Where(x => x is Way).Cast<Way>()
            .Where(w => w.Tags != null)
            .ToList();

        var closedWays = ways.Where(way => way.IsClosed()).ToList();
        var openWays = ways.Where(way => !way.IsClosed()).ToList();
        var nodes = source.Where(x => x is Node).Cast<Node>().ToList();
        
        // Landuse
        var parcels = closedWays
            .Select(x => LandParcelOsm.FromOsmWay(x, nodes) as LandParcelOsm)
            .Where(x => x is not null)
            .Cast<LandParcelOsm>()
            .ToList();

        // Buildings
        var buildings = closedWays
            .Select(x => BuildingOsm.FromOsmWay(x, nodes) as BuildingOsm)
            .Where(x => x is not null)
            .Cast<BuildingOsm>()
            .ToList();

        // Natural Areas
        var naturalAreas = closedWays
            .Select(x => NaturalClosedOsm.FromOsmWay(x, nodes) as NaturalClosedOsm)
            .Where(x => x is not null)
            .Cast<NaturalClosedOsm>()
            .ToList();

        // Misc Areas
        var miscAreas = closedWays
            .Select(x => MiscOsm.FromOsmWay(x, nodes) as MiscOsm)
            .Where(x => x is not null)
            .Cast<MiscOsm>()
            .ToList();

        // OPEN WAYS
        // Natural
        var naturalOpen = openWays
            .Select(x => NaturalOpenOsm.FromOsmWay(x, nodes) as NaturalOpenOsm)
            .Where(x => x is not null)
            .Cast<NaturalOpenOsm>()
            .ToList();

        // Waterways
        var waterways = openWays
            .Select(x => WaterwayOsm.FromOsmWay(x, nodes) as WaterwayOsm)
            .Where(x => x is not null)
            .Cast<WaterwayOsm>()
            .ToList();

        // Highways
        var transit = openWays
            .Select(x => TransitOsm.FromOsmWay(x, nodes) as TransitOsm)
            .Where(x => x is not null)
            .Cast<TransitOsm>()
            .ToList();

        // Boundary Barrier
        var boundaryBarrier = openWays
            .Select(x => BoundaryBarrierOsm.FromOsmWay(x, nodes) as BoundaryBarrierOsm)
            .Where(x => x is not null)
            .Cast<BoundaryBarrierOsm>()
            .ToList();

        // Power
        var powerOsm = openWays
            .Select(x => PowerOsm.FromOsmWay(x, nodes) as PowerOsm)
            .Where(x => x is not null)
            .Cast<PowerOsm>()
            .ToList();

        return new OsmModel(
            transit, 
            parcels, 
            buildings, 
            naturalAreas, 
            miscAreas,
            naturalOpen,
            waterways,
            boundaryBarrier,
            powerOsm);
    }
}
