using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;

namespace ArchSitePlan.Core.ModelEditActions.MetricsActions;
public class CalculateTransitMetricsAction : IModelEditAction
{
    public static TransitMetrics Run(Road road)
    {
        var length = road.Centerline.Length();
        var area = length * road.Width;

        return new TransitMetrics(
            road.Width,
            length,
            area,
            road.CenterLineId);
    }
}
