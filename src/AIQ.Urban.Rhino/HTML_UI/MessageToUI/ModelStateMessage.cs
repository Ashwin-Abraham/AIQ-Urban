using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

public class ModelStateMessage(UrbanModel urbanModel) : IMessageToUI
{
    public string Type => "ModelStateMessage";

    public List<LanduseInfo> Landuses { get; } = DefaultUses.All
        .Select(u => new LanduseInfo(u.Name, $"{u.Color.R},{u.Color.G},{u.Color.B}", u.Name))
        .ToList();

    public List<StudyAreaDataForUI> StudyAreaDataForUI { get; } = BuildStudyData(urbanModel);

    private static List<StudyAreaDataForUI> BuildStudyData(UrbanModel model)
    {
        var projectMetrics = model.ProjectMetrics;
        
        if (projectMetrics is null)
            return [];

        List<ZoneMetrics> zoneMetrics = [];
        var projectWideZoneMetrics = new ZoneMetrics(
            "All",
            projectMetrics.BuildingMetrics,
            projectMetrics.ParcelMetrics,
            model.Transits);
        
        zoneMetrics.Add(projectWideZoneMetrics);
        zoneMetrics.AddRange(projectMetrics.ZoneMetrics);

        return zoneMetrics.Select(z => ZoneToStudyData(z)).ToList();
    }

    private static StudyAreaDataForUI ZoneToStudyData(ZoneMetrics zone)
    {
        return new StudyAreaDataForUI(
            StudyName: zone.ZoneName,
            RoadLengthData: zone.TransitMetrics
                .Select(t => new ChartDataPoint($"{t.Type} {t.Width}m", t.RunningLength, ColorString(t.Type)))
                .ToList(),
            RoadAreaData: zone.TransitMetrics
                .Select(t => new ChartDataPoint($"{t.Type} {t.Width}m", t.Area, ColorString(t.Type)))
                .ToList(),
            UrbanLandAreaData: zone.BuiltAreaByUse
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

    private static string ColorString(Core.Enums.TransitType type) => type switch
    {
        TransitType.Road => "128,128,128",
        TransitType.Railway => "0,0,0",
        TransitType.CyclePath => "0,128,0",
        TransitType.Pedestrian => "160,82,45",
        TransitType.Waterway => "0,119,190",
        _ => "200,200,200"
    };
}
