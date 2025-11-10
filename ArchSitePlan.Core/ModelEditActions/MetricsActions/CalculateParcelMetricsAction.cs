using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;

namespace ArchSitePlan.Core.ModelEditActions.MetricsActions;
public class CalculateParcelMetricsAction(Parcel _parcel, List<Building> _buildingsOnParcel) 
    : IModelEditAction
{
    public ParcelMetrics Run()
    {
        var area = Math.Abs(_parcel.Boundary.Area());
        var perimeter = Math.Abs(_parcel.Boundary.ExternalBoundary.ILength());
        var use = _parcel.LandUse;
        var bua = _buildingsOnParcel.Sum(b => b.BUA());
        var coverageArea = _buildingsOnParcel.Sum(b => b.FootprintArea());

        var metrics = new ParcelMetrics
            (
            _parcel.CoreModelId,
            area,
            perimeter,
            use,
            bua,
            coverageArea);

        return metrics;
    }
}