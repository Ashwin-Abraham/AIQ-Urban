using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;

namespace ArchSitePlan.Core.ModelEditActions;
public class RefreshAllZoneContentsAndMetricsAction :IModelEditAction
{
    public List<(StudyArea Study, StudyAreaMetrics Metrics)> Run()
    {
        new RefreshAllZoneContentsAction().Run();
        return new RefreshAllZoneMetricsAction().Run();
    }
}
