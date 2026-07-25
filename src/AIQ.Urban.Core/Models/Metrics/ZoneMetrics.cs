using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.Models.Metrics;

public class ZoneMetrics : IMetrics
{
    public Guid SourceElementId { get; }
    public string ZoneName { get; }
    public double TotalLandArea { get; }
    public double TotalFootprintArea { get; }
    public double TotalBuiltArea { get; }
    public double TotalTransitLength { get; }
    public double TotalTransitArea { get; }
    public int BuildingCount { get; }
    public int ParcelCount { get; }
    public int TransitCount { get; }
    public List<(Use Use, double Area)> BuiltAreaByUse { get; }
    public List<(Use Use, double Area)> LandAreaByUse { get; }
    public List<TransitMetrics> TransitMetrics { get; }
    public double CoveragePercentage { get; }
    public double FAR { get; }

    public ZoneMetrics(
        Guid sourceElementId,
        string zoneName,
        List<BuildingMetrics> buildings,
        List<ParcelMetrics> parcels,
        IEnumerable<Transit> transits)
    {
        SourceElementId = sourceElementId;
        ZoneName = zoneName;

        BuildingCount = buildings.Count;
        ParcelCount = parcels.Count;

        TotalBuiltArea = buildings.Sum(b => b.BuiltUpArea);
        TotalFootprintArea = buildings.Sum(b => b.FootprintArea);
        TotalLandArea = parcels.Sum(p => p.LandArea);

        var transitList = transits.ToList();
        TransitCount = transitList.Count;

        TransitMetrics = transitList
            .Select(t => new TransitMetrics(t))
            .ToList();

        TotalTransitLength = TransitMetrics.Sum(t => t.RunningLength);
        TotalTransitArea = TransitMetrics.Sum(t => t.Area);

        BuiltAreaByUse = buildings
            .SelectMany(b => b.AreaPerUse)
            .GroupBy(kvp => kvp.Key)
            .Select(g => (g.Key, g.Sum(kvp => kvp.Value)))
            .ToList();

        LandAreaByUse = parcels
            .GroupBy(p => p.Use)
            .Select(g => (g.Key, g.Sum(p => p.LandArea)))
            .ToList();

        CoveragePercentage = TotalLandArea > 0 ? TotalFootprintArea / TotalLandArea * 100 : 0;
        FAR = TotalLandArea > 0 ? TotalBuiltArea / TotalLandArea : 0;
    }
}
