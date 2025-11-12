using ArchSitePlan.Core.Models;

namespace ArchSitePlan.Core.ModelEditActions.MetricsActions;
public class CalculateZoneMetricsAction(
    List<ParcelMetrics> _parcelMetrics,
    List<TransitMetrics> _transitMetrics,
    List<BuildingMetrics> _buildingMetrics)
{
    public StudyAreaMetrics Run(StudyArea zone)
    {
        var zoneMetrics = new StudyAreaMetrics(
            zone.Boundary,
            zone.Name,
            _parcelMetrics,
            _buildingMetrics,
            _transitMetrics);

       return zoneMetrics;
    }
}
