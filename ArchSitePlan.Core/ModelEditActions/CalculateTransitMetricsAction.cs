using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;

namespace ArchSitePlan.Core.ModelEditActions;
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
