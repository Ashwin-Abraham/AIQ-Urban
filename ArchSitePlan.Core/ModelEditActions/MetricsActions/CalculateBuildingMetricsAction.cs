using ArchSitePlan.Core.Models;

namespace ArchSitePlan.Core.ModelEditActions.MetricsActions;
public class CalculateBuildingMetricsAction(List<Building> _buildings) :IModelEditAction
{
    public List<(Building, BuildingMetrics)> Run()
    {
        var results = new List<(Building, BuildingMetrics)>();
        foreach (var building in _buildings)
        {
            var buildingMetrics = new BuildingMetrics(building);
            results.Add((building, buildingMetrics));
        }

        return results;
    }
}
