using AIQ.Exports.Excel.Reports;
using AIQ.Exports.Excel.Writing;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.Metrics;

namespace AIQ.Urban.RhinoInterface.Exports;

public static class UrbanMetricsExcelExporter
{
    public static void Export(
        UrbanModel urbanModel,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(urbanModel);

        var metrics = urbanModel.ProjectMetrics
            ?? throw new InvalidOperationException(
                "Project metrics have not been calculated. "
                + "Run the pipeline with metrics calculation before exporting.");

        var document = MetricsWorkbookBuilder.Build(
            BuildWorkbookData(metrics));
        new ExcelDocumentWriter().Write(document, filePath);
    }

    internal static MetricsWorkbookData BuildWorkbookData(
        ProjectMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        var worksheets = new List<MetricsWorksheetData>
        {
            BuildProjectWorksheet(metrics),
        };
        worksheets.AddRange(metrics.ZoneMetrics.Select(BuildZoneWorksheet));

        return new MetricsWorkbookData(worksheets);
    }

    private static MetricsWorksheetData BuildProjectWorksheet(
        ProjectMetrics metrics)
    {
        return new MetricsWorksheetData(
            Name: "Project",
            TotalLandArea: metrics.TotalLandArea,
            TotalFootprintArea: metrics.BuildingMetrics.Sum(
                building => building.FootprintArea),
            TotalBuiltArea: metrics.TotalBuiltUpArea,
            TotalTransitLength: metrics.TotalTransitLength,
            TotalTransitArea: metrics.TotalTransitArea,
            BuildingCount: metrics.BuildingCount,
            ParcelCount: metrics.ParcelCount,
            TransitCount: metrics.TransitMetrics.Count,
            CoverageRatio: metrics.OverallCoverageRatio / 100,
            FAR: metrics.OverallFAR,
            LandAreaByUse: MapUseAreas(metrics.LandAreaByUse),
            BuiltAreaByUse: MapUseAreas(metrics.BuiltAreaByUse),
            TransitMetrics: MapTransitMetrics(metrics.TransitMetrics));
    }

    private static MetricsWorksheetData BuildZoneWorksheet(
        ZoneMetrics metrics)
    {
        return new MetricsWorksheetData(
            Name: metrics.ZoneName,
            TotalLandArea: metrics.TotalLandArea,
            TotalFootprintArea: metrics.TotalFootprintArea,
            TotalBuiltArea: metrics.TotalBuiltArea,
            TotalTransitLength: metrics.TotalTransitLength,
            TotalTransitArea: metrics.TotalTransitArea,
            BuildingCount: metrics.BuildingCount,
            ParcelCount: metrics.ParcelCount,
            TransitCount: metrics.TransitCount,
            CoverageRatio: metrics.CoveragePercentage / 100,
            FAR: metrics.FAR,
            LandAreaByUse: MapUseAreas(metrics.LandAreaByUse),
            BuiltAreaByUse: MapUseAreas(metrics.BuiltAreaByUse),
            TransitMetrics: MapTransitMetrics(metrics.TransitMetrics));
    }

    private static List<UseAreaData> MapUseAreas(
        IEnumerable<(Use Use, double Area)> areas)
    {
        return areas
            .Select(item => new UseAreaData(
                item.Use.Name,
                item.Use.Color,
                item.Area))
            .ToList();
    }

    private static List<TransitMetricData> MapTransitMetrics(
        IEnumerable<TransitMetrics> metrics)
    {
        return metrics
            .Select(metric => new TransitMetricData(
                metric.Type.ToString(),
                metric.Width,
                metric.RunningLength,
                metric.Area))
            .ToList();
    }
}
