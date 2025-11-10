namespace ArchSitePlan.Core.Models;

public record ParcelMetrics(
    Guid ParcelId,
    double Area,
    double Perimeter,
    Landuse Use,
    double BUA,
    double CoverageArea);

public static class ParcelMetricsExtension
{
    public static double FAR(this ParcelMetrics metrics) => metrics.BUA / metrics.Area;

    public static double CoverageRatio(this ParcelMetrics metrics) => metrics.CoverageArea / metrics.Area;
}