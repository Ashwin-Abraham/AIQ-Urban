using System.Drawing;
using AIQ.Exports.Excel.Reports;
using AIQ.Exports.Excel.Writing;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.Metrics;

namespace AIQ.Urban.RhinoInterface.Exports;

public sealed class UrbanMetricsExcelExporter : IUrbanMetricsExcelExporter
{
    public void Export(
        UrbanModel urbanModel,
        string filePath,
        string projectName,
        DateTime exportedAt)
    {
        var report = BuildWorkbookData(
            urbanModel,
            projectName,
            exportedAt);
        var document = MetricsWorkbookBuilder.Build(report);
        new ExcelDocumentWriter().Write(document, filePath);
    }

    internal static MetricsWorkbookData BuildWorkbookData(
        UrbanModel urbanModel,
        string projectName,
        DateTime exportedAt)
    {
        ArgumentNullException.ThrowIfNull(urbanModel);

        var metrics = urbanModel.ProjectMetrics
            ?? throw new InvalidOperationException(
                "Project metrics have not been calculated. "
                + "Run the pipeline with metrics calculation before exporting.");

        var transitColors = urbanModel.Transits
            .GroupBy(transit => (transit.TransitType, transit.Width))
            .ToDictionary(
                group => group.Key,
                group => group.First().Color ?? Color.DarkGray);

        var project = new MetricsScopeData(
            DisplayName: projectName,
            ZoneBoundaryArea: null,
            ParcelArea: metrics.TotalLandArea,
            BuildingFootprint: metrics.BuildingMetrics.Sum(
                building => building.FootprintArea),
            BuiltArea: metrics.TotalBuiltUpArea,
            TransitLength: metrics.TotalTransitLength,
            TransitArea: metrics.TotalTransitArea,
            BuildingCount: metrics.BuildingCount,
            ParcelCount: metrics.ParcelCount,
            TransitCount: metrics.TransitMetrics.Count,
            ParcelUses: MapParcelUses(metrics.ParcelMetrics),
            BuiltPlateUses: MapBuiltPlateUses(metrics.BuiltAreaByUse),
            TransitMetrics: MapTransitMetrics(
                metrics.TransitMetrics,
                transitColors));

        var zones = metrics.ZoneMetrics
            .Select(zone => MapZone(zone, transitColors))
            .ToList();

        return new MetricsWorkbookData(
            projectName,
            exportedAt,
            project,
            zones);
    }

    private static MetricsScopeData MapZone(
        ZoneMetrics metrics,
        IReadOnlyDictionary<(TransitType Type, double Width), Color> transitColors)
    {
        return new MetricsScopeData(
            DisplayName: metrics.ZoneName,
            ZoneBoundaryArea: metrics.ZoneBoundaryArea,
            ParcelArea: metrics.TotalLandArea,
            BuildingFootprint: metrics.TotalFootprintArea,
            BuiltArea: metrics.TotalBuiltArea,
            TransitLength: metrics.TotalTransitLength,
            TransitArea: metrics.TotalTransitArea,
            BuildingCount: metrics.BuildingCount,
            ParcelCount: metrics.ParcelCount,
            TransitCount: metrics.TransitCount,
            ParcelUses: MapParcelUses(metrics.ParcelMetrics),
            BuiltPlateUses: MapBuiltPlateUses(metrics.BuiltAreaByUse),
            TransitMetrics: MapTransitMetrics(
                metrics.TransitMetrics,
                transitColors));
    }

    private static List<ParcelUseData> MapParcelUses(
        IEnumerable<ParcelMetrics> parcels)
    {
        return parcels
            .GroupBy(parcel => parcel.Use)
            .Select(group => new ParcelUseData(
                group.Key.Name,
                group.Key.Color,
                group.Count(),
                group.Sum(parcel => parcel.LandArea),
                group.Sum(parcel => parcel.Perimeter),
                group.Sum(parcel => parcel.BuiltFootprint)))
            .OrderBy(row => row.UseName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<BuiltPlateUseData> MapBuiltPlateUses(
        IEnumerable<(Use Use, double Area)> areas)
    {
        return areas
            .Select(item => new BuiltPlateUseData(
                item.Use.Name,
                item.Use.Color,
                item.Area))
            .OrderBy(row => row.UseName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<TransitMetricData> MapTransitMetrics(
        IEnumerable<TransitMetrics> metrics,
        IReadOnlyDictionary<(TransitType Type, double Width), Color> transitColors)
    {
        return metrics
            .Select(metric => new TransitMetricData(
                metric.Type.ToString(),
                metric.Width,
                metric.RunningLength,
                metric.Area,
                transitColors.GetValueOrDefault(
                    (metric.Type, metric.Width),
                    Color.DarkGray)))
            .ToList();
    }
}
