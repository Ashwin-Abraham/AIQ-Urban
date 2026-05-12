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

    /// <summary>Total built-up area grouped by use name.</summary>
    [JsonPropertyName("areaPerUse")]
    public Dictionary<string, double> AreaPerUse { get; }

    /// <summary>Construct a snapshot from pre-computed per-element metrics.</summary>
    public ProjectMetrics(
        List<BuildingMetrics> buildingMetrics,
        List<ParcelMetrics> parcelMetrics)
    {
        BuildingMetrics = buildingMetrics;
        ParcelMetrics = parcelMetrics;

        BuildingCount = buildingMetrics.Count;
        ParcelCount = parcelMetrics.Count;
        TotalBuiltUpArea = buildingMetrics.Sum(b => b.BuiltUpArea);
        TotalLandArea = parcelMetrics.Sum(p => p.LandArea);
        OverallFAR = TotalLandArea > 0 ? TotalBuiltUpArea / TotalLandArea : 0;
        OverallCoverageRatio = TotalLandArea > 0
            ? parcelMetrics.Sum(p => p.BuiltFootprint) / TotalLandArea * 100
            : 0;

        AreaPerUse = buildingMetrics
            .SelectMany(b => b.AreaPerUse)
            .GroupBy(kvp => kvp.Key.Name)
            .ToDictionary(g => g.Key, g => g.Sum(kvp => kvp.Value), StringComparer.Ordinal);
    }
}
