using AIQ.Primitives.Extensions;
using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Models;
using System.Drawing;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
public record StudyAreasDataForUI(
        string StudyName,
        List<GraphData> RoadLengthData,
        List<GraphData> RoadAreaData,
        List<GraphData> UrbanLandAreaData,
        List<GraphData> UrbanBuiltAreaData,
        double TotalArea,
        double TotalBuiltArea,
        double FAR,
        double LandCoverage);
public class ModelStateMessage :IMessageToUI
{
    public string Type { get; } = nameof(ModelStateMessage);
    public List<StudyAreasDataForUI> StudyAreaDataForUI { get; private set; }
    public List<Landuse> Landuses { get; private set; }

    public ModelStateMessage(List<(StudyArea Zone, StudyAreaMetrics Metrics)> studyAndMetrics)
    {
        StudyAreaDataForUI = studyAndMetrics
            .Select(x => ConvertToStudyForUI(x.Zone, x.Metrics))
            .ToList();

        Landuses = CoreModelSingelton.Instance.Landuses;
    }

    private static StudyAreasDataForUI ConvertToStudyForUI(StudyArea zone, StudyAreaMetrics metrics)
    {
        return new(
            zone.Name,
            RoadLengthData: ExtractRoadLengthGraphData(metrics),
            RoadAreaData: ExtractRoadAreaGraphData(metrics),
            UrbanLandAreaData: ExtractLandUseGraphData(metrics),
            UrbanBuiltAreaData: ExtractBuiltUseGraphData(metrics),
            metrics.DevelopableArea,
            metrics.BuiltArea,
            metrics.FAR,
            metrics.CoverageRatio);
    }

    private static List<GraphData> ExtractLandUseGraphData(StudyAreaMetrics metrics)
    {
        var graphData = new List<GraphData>();

        var metricsByUse = metrics.ParcelMetrics.GroupBy(p => p.Parcel.LandUse);

        foreach (var group in metricsByUse)
        {
            graphData.Add(
                new GraphData(
                    Label: group.Key.Name,
                    Data: group.Sum(u => u.Metrics.Area),
                    Color: ColorToRGBA(group.Key.Color)
                    ));
        }

        return graphData;
    }

    private static string ColorToRGBA(Color color)
    {
        return $"rgba({color.R}, {color.G}, {color.B}, {color.A})";
    }

    private static List<GraphData> ExtractBuiltUseGraphData(StudyAreaMetrics metrics)
    {
        var graphData = new List<GraphData>();

        var buaByUse = metrics.BuildingMetrics
            .SelectMany(x => x.Metrics.BuaPerUse)
            .GroupBy(x  => x.Use)
            .Select(x => (x.Key, x.Sum(x => x.BUA)))
            .ToList();

        foreach (var group in buaByUse)
        {
            graphData.Add(
                new GraphData(
                    Label: group.Key?.Name?? "null",
                    Data: group.Item2,
                    Color: ColorToRGBA(group.Key?.Color?? Color.Gray)
                    ));
        }

        return graphData;
    }

    private static List<GraphData> ExtractRoadLengthGraphData(StudyAreaMetrics metrics)
    {
        var groupedByWidth = metrics.TransitMetrics.GroupBy(p => Math.Round(p.Metrics.RoadWidth, 1));
        var graphData = new List<GraphData>();
        int colorIndex = 0;

        foreach (var transit in groupedByWidth)
        {
            if (colorIndex > AppColours.StandardColors.Count - 1)
                colorIndex = 0;

            var color = AppColours.StandardColors[colorIndex].ToRGB();
            colorIndex++;

            graphData.Add(
                new GraphData(
                    Label: transit.Key.ToString(),
                    Data: transit.Sum(u => u.Metrics.RunningLength),
                    Color: ColorToRGBA(color)
                    ));

        }

        return graphData;
    }

    private static List<GraphData> ExtractRoadAreaGraphData(StudyAreaMetrics metrics)
    {
        var groupedByWidth = metrics.TransitMetrics.GroupBy(p => Math.Round(p.Metrics.RoadWidth, 1));
        var graphData = new List<GraphData>();
        int colorIndex = 0;

        foreach (var transit in groupedByWidth)
        {
            if (colorIndex > AppColours.StandardColors.Count - 1)
                colorIndex = 0;

            var color = AppColours.StandardColors[colorIndex].ToRGB();
            colorIndex++;

            graphData.Add(
                new GraphData(
                    Label: transit.Key.ToString(),
                    Data: transit.Sum(u => u.Metrics.Area),
                    Color: ColorToRGBA(color)
                    ));
        }

        return graphData;
    }
}
