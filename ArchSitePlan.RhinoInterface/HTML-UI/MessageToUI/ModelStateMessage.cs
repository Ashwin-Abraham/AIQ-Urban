using AIQ.Primitives.Extensions;
using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Metrics;
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

    public ModelStateMessage()
    {
        StudyAreaDataForUI = CoreModelSingelton.Instance.Studies
            .Select(x => RefreshActiveStudyAndMetrics(x))
            .ToList();

        Landuses = CoreModelSingelton.Instance.Landuses;
    }

    private static StudyAreasDataForUI RefreshActiveStudyAndMetrics(StudyArea study)
    {
        return new(
            study.Name,
            RoadLengthData: ExtractRoadLengthGraphData(study.Metrics),
            RoadAreaData: ExtractRoadAreaGraphData(study.Metrics),
            UrbanLandAreaData: ExtractLandUseGraphData(study.Metrics),
            UrbanBuiltAreaData: ExtractBuiltUseGraphData(study.Metrics),
            study.Metrics.BoundedArea,
            study.Metrics.BuiltArea,
            study.Metrics.FAR,
            study.Metrics.CoverageRatio);
    }

    private static List<GraphData> ExtractLandUseGraphData(StudyAreaMetrics metrics)
    {
        var graphData = new List<GraphData>();

        var metricsByUse = metrics.ParcelMetrics.GroupBy(p => p.Use);

        foreach (var group in metricsByUse)
        {
            graphData.Add(
                new GraphData(
                    Label: group.Key.Name,
                    Data: group.Sum(u => u.Area),
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

        var metricsByUse = metrics.ParcelMetrics.GroupBy(p => p.Use);

        foreach (var use in metricsByUse)
        {
            graphData.Add(
                new GraphData(
                    Label: use.Key.Name,
                    Data: use.Sum(u => u.BUA),
                    Color: ColorToRGBA(use.Key.Color)
                    ));
        }

        return graphData;
    }

    private static List<GraphData> ExtractRoadLengthGraphData(StudyAreaMetrics metrics)
    {
        var groupedByWidth = metrics.TransitMetrics.GroupBy(p => Math.Round(p.RoadWidth, 1));
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
                    Data: transit.Sum(u => u.RunningLength),
                    Color: ColorToRGBA(color)
                    ));

        }

        return graphData;
    }

    private static List<GraphData> ExtractRoadAreaGraphData(StudyAreaMetrics metrics)
    {
        var groupedByWidth = metrics.TransitMetrics.GroupBy(p => Math.Round(p.RoadWidth, 1));
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
                    Data: transit.Sum(u => u.Area),
                    Color: ColorToRGBA(color)
                    ));
        }

        return graphData;
    }
}
