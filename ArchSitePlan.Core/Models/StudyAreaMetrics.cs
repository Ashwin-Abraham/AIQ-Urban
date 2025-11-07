using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models;

public class StudyAreaMetrics
{
    public double BoundedArea { get; private set; } = 25;
    public double FAR { get; private set; } = 12;
    public double DevelopableArea { get; private set; } = 5;
    public double BuiltArea { get; private set; } = 2;
    public double BuiltCoverageArea { get; private set; } = 0;
    public double CoverageRatio { get; private set; } = 0;

    public IReadOnlyList<(Parcel Parcel, ParcelMetrics Metrics)> ParcelMetrics { get; private set; }

    public IReadOnlyList<(Building Building, BuildingMetrics Metrics)> BuildingMetrics { get; private set; }

    public IReadOnlyList<(Road Transit, TransitMetrics Metrics)> TransitMetrics { get; private set; }

    public StudyAreaMetrics(
        ICurve? boundary, 
        List<(Parcel, ParcelMetrics)> parcelMetrics, 
        List<(Building, BuildingMetrics)> buildingsMetrics,
        List<(Road, TransitMetrics)> transitMetrics)
    {
        ParcelMetrics = parcelMetrics;
        BuildingMetrics = buildingsMetrics;
        TransitMetrics = transitMetrics;

        BoundedArea = Math.Abs(boundary?.IArea()?? 0);
        DevelopableArea = ParcelMetrics.Sum(p => p.Metrics.Area);
        BuiltArea = BuildingMetrics.Sum(b => b.Building.BUA());
        FAR = BuiltArea / DevelopableArea;
        BuiltCoverageArea = SetBuiltCoverage();
        CoverageRatio = BuiltCoverageArea / DevelopableArea;
    }

    private double SetBuiltCoverage()
    {
        var buildingCoverage = BuildingMetrics
            .Select(x => x.Building.FloorPlates.MinBy(f => f.Profile.ExternalBoundary.IStartPoint().Z))
            .Sum(floor => floor?.Profile.ExternalBoundary.IArea()?? 0);

        return buildingCoverage;
    }
}
