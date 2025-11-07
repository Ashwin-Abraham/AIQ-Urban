using ArchSitePlan.Core.Models;

namespace ArchSitePlan.Core.Metrics;

public class ParcelMetrics
{
    public double Area { get; init; }
    public double Perimeter { get; init; }
    public Landuse Use { get; init; }
    public double BUA { get; init; }
    public double FAR { get; init; }
    public double CoverageArea { get; init; }
    public double CoverageRatio { get; init; }
}