using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;

namespace ArchSitePlan.Core.ModelEditActions;
public class CalculateZoneMetricsAction(
    List<(Parcel, ParcelMetrics)> _parcelMetrics,
    List<(Road, TransitMetrics)> _transitMetrics,
    List<(Building, BuildingMetrics)> _buildingMetrics)
{
    public StudyAreaMetrics Run(StudyArea zone)
    {
        var zoneMetrics = new StudyAreaMetrics(
            zone.Boundary,
            _parcelMetrics,
            _buildingMetrics,
            _transitMetrics);

       return zoneMetrics;
    }
}
