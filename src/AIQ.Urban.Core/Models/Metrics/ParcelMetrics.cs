using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.Models.Metrics;
public class ParcelMetrics : IMetrics
{
    public Guid SourceElementId { get; }
    public Use Use { get; }
    public double LandArea { get; }
    public double Perimeter { get; }
    public double BuiltUpArea { get; }
    public double BuiltFootprint { get; }
    public double FAR { get; }
    public double CoverageRatio { get; }

    public ParcelMetrics(
        Parcel parcel,
        List<BuildingMetrics> buildingsOnParcel)
    {
        SourceElementId = parcel.Id;
        Use = parcel.Use;
        LandArea = parcel.Outline.Area();
        Perimeter = parcel.Outline.Perimeter();
        BuiltUpArea = buildingsOnParcel.Sum(x => x.BuiltUpArea);
        BuiltFootprint = buildingsOnParcel.Sum(x => x.FootprintArea);
        FAR = BuiltUpArea / LandArea;
        CoverageRatio = BuiltFootprint / LandArea * 100;
    }
}
