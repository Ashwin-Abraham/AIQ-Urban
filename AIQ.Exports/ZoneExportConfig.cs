using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Utils;

namespace AIQ.Exports;
public record ZoneExportConfig
    (StudyAreaMetrics Metrics,
    string PlanImagePath);

public static class ZoneExportConfigExtensions
{
    public static List<GraphData> ExtractUniqueLandUseBasedGraphData(this ZoneExportConfig config)
    {
        var landUses = config.Metrics.ExtractLandAreaGraphData();
        var builtUses = config.Metrics.ExtractBuiltUseGraphData();

        return landUses.Concat(builtUses)
            .DistinctBy(x => x.Label)
            .ToList();
    }
}