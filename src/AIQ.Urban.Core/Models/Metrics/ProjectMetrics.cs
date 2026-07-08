using System.Text.Json.Serialization;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.Models.Metrics;

/// <summary>Aggregate metrics for the entire urban model. Captures a snapshot of all building and parcel metrics plus project-level roll-ups.</summary>
public class ProjectMetrics
{
    /// <summary>Metrics for every building in the model.</summary>
    [JsonIgnore]
    public List<BuildingMetrics> BuildingMetrics { get; }

    /// <summary>Metrics for every parcel in the model.</summary>
    [JsonIgnore]
    public List<ParcelMetrics> ParcelMetrics { get; }

    /// <summary>Aggregated transit metrics grouped by (Type, Width).</summary>
    [JsonPropertyName("transitMetrics")]
    public List<TransitMetrics> TransitMetrics { get; }

    /// <summary>Metrics per zone in the model.</summary>
    [JsonPropertyName("zoneMetrics")]
    public List<ZoneMetrics> ZoneMetrics { get; }

    /// <summary>Sum of all building built-up areas.</summary>
    [JsonPropertyName("totalBuiltUpArea")]
    public double TotalBuiltUpArea { get; }

    /// <summary>Sum of all parcel land areas.</summary>
    [JsonPropertyName("totalLandArea")]
    public double TotalLandArea { get; }

    /// <summary>Total built-up area divided by total land area (FAR).</summary>
    [JsonPropertyName("overallFAR")]
    public double OverallFAR { get; }

    /// <summary>Total building footprint divided by total land area, as a percentage.</summary>
    [JsonPropertyName("overallCoverageRatio")]
    public double OverallCoverageRatio { get; }

    /// <summary>Number of buildings in the model.</summary>
    [JsonPropertyName("buildingCount")]
    public int BuildingCount { get; }

    /// <summary>Number of parcels in the model.</summary>
    [JsonPropertyName("parcelCount")]
    public int ParcelCount { get; }

    /// <summary>Sum of all transit running lengths.</summary>
    [JsonPropertyName("totalTransitLength")]
    public double TotalTransitLength { get; }

    /// <summary>Sum of all transit right-of-way areas.</summary>
    [JsonPropertyName("totalTransitArea")]
    public double TotalTransitArea { get; }

    /// <summary>Total built-up area grouped by use.</summary>
    [JsonPropertyName("areaPerUse")]
    public List<(Use Use, double Area)> BuiltAreaByUse { get; }

    /// <summary>Total land area grouped by use.</summary>
    [JsonPropertyName("landAreaByUse")]
    public List<(Use Use, double Area)> LandAreaByUse { get; }

    /// <summary>Construct a snapshot from pre-computed per-element metrics.</summary>
    public ProjectMetrics(
        List<BuildingMetrics> buildingMetrics,
        List<ParcelMetrics> parcelMetrics)
        : this(buildingMetrics, parcelMetrics, [], [])
    {
    }

    /// <summary>Construct a snapshot from pre-computed per-element metrics.</summary>
    public ProjectMetrics(
        List<BuildingMetrics> buildingMetrics,
        List<ParcelMetrics> parcelMetrics,
        List<TransitMetrics> transitMetrics)
        : this(buildingMetrics, parcelMetrics, transitMetrics, [])
    {
    }

    /// <summary>Construct a snapshot from pre-computed per-element metrics.</summary>
    public ProjectMetrics(
        List<BuildingMetrics> buildingMetrics,
        List<ParcelMetrics> parcelMetrics,
        List<TransitMetrics> transitMetrics,
        List<ZoneMetrics> zoneMetrics)
    {
        BuildingMetrics = buildingMetrics;
        ParcelMetrics = parcelMetrics;
        TransitMetrics = transitMetrics;
        ZoneMetrics = zoneMetrics;

        BuildingCount = buildingMetrics.Count;
        ParcelCount = parcelMetrics.Count;
        TotalBuiltUpArea = buildingMetrics.Sum(b => b.BuiltUpArea);
        TotalLandArea = parcelMetrics.Sum(p => p.LandArea);
        OverallFAR = TotalLandArea > 0 ? TotalBuiltUpArea / TotalLandArea : 0;
        OverallCoverageRatio = TotalLandArea > 0
            ? parcelMetrics.Sum(p => p.BuiltFootprint) / TotalLandArea * 100
            : 0;

        BuiltAreaByUse = buildingMetrics
            .SelectMany(b => b.AreaPerUse)
            .GroupBy(kvp => kvp.Key)
            .Select(g => (g.Key, g.Sum(kvp => kvp.Value)))
            .ToList();

        LandAreaByUse = parcelMetrics
            .GroupBy(p => p.Use)
            .Select(g => (g.Key, g.Sum(p => p.LandArea)))
            .ToList();

        TotalTransitLength = transitMetrics.Sum(t => t.RunningLength);
        TotalTransitArea = transitMetrics.Sum(t => t.Area);
    }
}
