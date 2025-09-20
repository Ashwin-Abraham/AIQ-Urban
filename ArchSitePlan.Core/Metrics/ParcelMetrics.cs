using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.Engine.Spatial;

namespace ArchSitePlan.Core.Metrics;

public class ParcelMetrics
{
    public double Area;
    public double Perimeter;
    public Landuse Use;
    public double BUA;
    public double FAR;
    public double CoverageArea;
    public double CoverageRatio;
    public ParcelMetrics(Parcel parcel, List<Building> buildingsOnParcel) 
    {
        CalculateParcelMetrics(parcel, buildingsOnParcel);
        Use = parcel.LandUse;
    }

    private void CalculateParcelMetrics(Parcel parcel, List<Building> buildingsOnParcel)
    {
        Area = Math.Abs(parcel.Boundary.Area());
        Perimeter = Math.Abs(parcel.Boundary.ExternalBoundary.Length());
        Use = parcel.LandUse;
        BUA = buildingsOnParcel.Sum(b => b.BUA());
        FAR = BUA / Area;
        CoverageArea = buildingsOnParcel.Sum(b => b.FootprintArea());
        CoverageRatio = CoverageArea / Area;
    }
}
