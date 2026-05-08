using ArchSitePlan.Core.Utils;

namespace AIQ.Exports;
public record PowerpointExportConfig(
    string SaveLocation,
    string AuthorLogoPath,
    string AuthorName,
    string ProjectName,
    ZoneExportConfig FullProjectExportConfig,
    List<ZoneExportConfig> OtherZoneExportConfigs
    );

public static class PowerpointExportConfigExtensions
{
    public static List<GraphData> ExtractUniqueLandUseBasedGraphData(this PowerpointExportConfig config)
    {
        return config.OtherZoneExportConfigs
            .SelectMany(x => x.ExtractUniqueLandUseBasedGraphData())
            .Concat(config.FullProjectExportConfig.ExtractUniqueLandUseBasedGraphData())
            .DistinctBy(x => x.Label)
            .ToList();
    }
}
