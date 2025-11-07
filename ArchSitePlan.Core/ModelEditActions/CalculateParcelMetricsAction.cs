using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;

namespace ArchSitePlan.Core.ModelEditActions;
public class CalculateParcelMetricsAction(Parcel _parcel, List<Building> _buildingsOnParcel) 
    : IModelEditAction
{
    public (Parcel parcel, ParcelMetrics Metrics) Run()
    {
        var area = Math.Abs(_parcel.Boundary.Area());
        var perimeter = Math.Abs(_parcel.Boundary.ExternalBoundary.ILength());
        var use = _parcel.LandUse;
        var bua = _buildingsOnParcel.Sum(b => b.BUA());
        var far = bua / area;
        var coverageArea = _buildingsOnParcel.Sum(b => b.FootprintArea());
        var coverageRatio = coverageArea / area;

        var metrics = new ParcelMetrics
        {
            Area = area,
            Perimeter = perimeter,
            Use = use,
            BUA = bua,
            FAR = far,
            CoverageArea = coverageArea,
            CoverageRatio = coverageRatio
        };

        return (_parcel, metrics);
    }
}