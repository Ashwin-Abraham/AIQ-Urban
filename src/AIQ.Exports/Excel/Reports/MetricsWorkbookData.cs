using System.Drawing;

namespace AIQ.Exports.Excel.Reports;

public sealed record MetricsWorkbookData(
    string ProjectName,
    DateTime ExportedAt,
    MetricsScopeData Project,
    IReadOnlyList<MetricsScopeData> Zones);

public sealed record MetricsScopeData(
    string DisplayName,
    double? ZoneBoundaryArea,
    double ParcelArea,
    double BuildingFootprint,
    double BuiltArea,
    double TransitLength,
    double TransitArea,
    int BuildingCount,
    int ParcelCount,
    int TransitCount,
    IReadOnlyList<ParcelUseData> ParcelUses,
    IReadOnlyList<BuiltPlateUseData> BuiltPlateUses,
    IReadOnlyList<TransitMetricData> TransitMetrics);

public sealed record ParcelUseData(
    string UseName,
    Color Color,
    int ParcelCount,
    double Area,
    double SumOfParcelPerimeters,
    double BuildingFootprint);

public sealed record BuiltPlateUseData(
    string UseName,
    Color Color,
    double BuiltArea);

public sealed record TransitMetricData(
    string Type,
    double Width,
    double RunningLength,
    double Area,
    Color Color);
