using ArchSitePlan.Core.Models;

namespace ArchSitePlan.Core.ModelEditActions.MetricsActions;
public class CalculateBuildingMetricsAction :IModelEditAction
{
    public static BuildingMetrics Run(Building building)
    {
        return new BuildingMetrics(building);
    }
}
