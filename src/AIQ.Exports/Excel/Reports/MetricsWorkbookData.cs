using System.Drawing;

namespace AIQ.Exports.Excel.Reports;

public sealed record MetricsWorkbookData(
    IReadOnlyList<MetricsWorksheetData> Worksheets);

public sealed record MetricsWorksheetData(
    string Name,
    double TotalLandArea,
    double TotalFootprintArea,
    double TotalBuiltArea,
    double TotalTransitLength,
    double TotalTransitArea,
    int BuildingCount,
    int ParcelCount,
    int TransitCount,
    double CoverageRatio,
    double FAR,
    IReadOnlyList<UseAreaData> LandAreaByUse,
    IReadOnlyList<UseAreaData> BuiltAreaByUse,
    IReadOnlyList<TransitMetricData> TransitMetrics);

public sealed record UseAreaData(
    string UseName,
    Color Color,
    double Area);

public sealed record TransitMetricData(
    string Type,
    double Width,
    double RunningLength,
    double Area);
