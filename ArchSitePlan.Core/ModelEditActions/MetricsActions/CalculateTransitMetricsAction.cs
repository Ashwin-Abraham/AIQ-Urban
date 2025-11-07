using ArchSitePlan.Core.Models;

namespace ArchSitePlan.Core.ModelEditActions.MetricsActions;
public class CalculateTransitMetricsAction(List<Road> _transit) : IModelEditAction
{
    public List<(Road, TransitMetrics)> Run()
    {
        var transitAndMetrics = new List<(Road, TransitMetrics)>();

        foreach(var road in _transit)
            transitAndMetrics.Add((road, new TransitMetrics(road)));

        return transitAndMetrics;
    }
}
