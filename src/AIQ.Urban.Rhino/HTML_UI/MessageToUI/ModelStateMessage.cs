using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.Primitives.Extensions;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.Metrics;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

public record ChartDataPoint(string Label, double Data, string Color);

public record StudyAreaDataForUI(
    string StudyName,
    List<ChartDataPoint> RoadLengthData,
    List<ChartDataPoint> RoadAreaData,
    List<ChartDataPoint> UrbanLandAreaData,
    List<ChartDataPoint> UrbanBuiltAreaData,
    double TotalArea,
    double TotalBuiltArea,
    double FAR,
    double LandCoverage);

public record LanduseInfo(string Name, string Color, string Id);

public class ModelStateMessage : IMessageToUI
{
    public string Type => "ModelStateMessage";

    public List<LanduseInfo> Landuses { get; }

    public List<StudyAreaDataForUI> StudyAreaDataForUI { get; init; }

    private readonly List<(TransitType Type, double Width, Color Color)> _colorMap;

    public ModelStateMessage(UrbanModel urbanModel)
    {
        _colorMap = BuildMap(urbanModel);
        Landuses = urbanModel.Uses
        .Select(u => new LanduseInfo(u.Name, $"{u.Color.R},{u.Color.G},{u.Color.B}", u.Id.ToString()))
        .ToList();

        StudyAreaDataForUI = BuildStudyData(urbanModel);
    }

    private static List<(TransitType Type, double Width, Color Color)> BuildMap(UrbanModel urbanModel)
    {
        List<(TransitType Type, double Width, Color Color)> map = [];

        foreach (var transit in urbanModel.Transits)
        {
            if (map.Any(x => x.Type == transit.TransitType && x.Width.AlmostEquals(transit.Width)))
                continue;

            var color = transit.Color.HasValue ? transit.Color.Value : Color.DarkGray;
            map.Add((transit.TransitType, transit.Width, color));
        }

        return map;
    }

    private List<StudyAreaDataForUI> BuildStudyData(UrbanModel model)
    {
        var projectMetrics = model.ProjectMetrics;
        
        if (projectMetrics is null)
            return [];

        List<ZoneMetrics> zoneMetrics = [];
        var projectWideZoneMetrics = new ZoneMetrics(
            Guid.Empty,
            "All",
            projectMetrics.BuildingMetrics,
            projectMetrics.ParcelMetrics,
            model.Transits);
        
        zoneMetrics.Add(projectWideZoneMetrics);
        zoneMetrics.AddRange(projectMetrics.ZoneMetrics);

        return zoneMetrics.Select(z => ZoneToStudyData(z)).ToList();
    }

    private StudyAreaDataForUI ZoneToStudyData(ZoneMetrics zone)
    {
        var groupedTransitMetrics = zone.TransitMetrics
            .GroupBy(t => (t.Type, t.Width))
            .Select(group => new
            {
                group.Key.Type,
                group.Key.Width,
                RunningLength = group.Sum(t => t.RunningLength),
                Area = group.Sum(t => t.Area),
            })
            .ToList();

        return new StudyAreaDataForUI(
            StudyName: zone.ZoneName,
            RoadLengthData: groupedTransitMetrics
                .Select(t => new ChartDataPoint($"{t.Type} {t.Width}m", t.RunningLength, ColorString(t.Type, t.Width, _colorMap)))
                .ToList(),
            RoadAreaData: groupedTransitMetrics
                .Select(t => new ChartDataPoint($"{t.Type} {t.Width}m", t.Area, ColorString(t.Type, t.Width, _colorMap)))
                .ToList(),
            UrbanLandAreaData: zone.LandAreaByUse
                .Select(b => new ChartDataPoint(b.Use.Name, b.Area, $"{b.Use.Color.R},{b.Use.Color.G},{b.Use.Color.B}"))
                .ToList(),
            UrbanBuiltAreaData: zone.BuiltAreaByUse
                .Select(b => new ChartDataPoint(b.Use.Name, b.Area, $"{b.Use.Color.R},{b.Use.Color.G},{b.Use.Color.B}"))
                .ToList(),
            TotalArea: zone.TotalLandArea,
            TotalBuiltArea: zone.TotalBuiltArea,
            FAR: zone.FAR,
            LandCoverage: zone.CoveragePercentage);
    }

    private static string ColorString(TransitType type, double width, List<(TransitType Type, double Width, Color Color)> colorMap)
    {
        foreach (var (mapType, mapWidth, mapColor) in colorMap)
        {
            if (mapType == type && mapWidth.AlmostEquals(width, 1e-3))
                return $"{mapColor.R},{mapColor.G},{mapColor.B}";
        }
        return "200,200,200";
    }
}
