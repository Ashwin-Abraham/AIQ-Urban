using AIQ.Primitives.Extensions;
using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;

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
public class StudyAreaMetricsMessage :IMessageToUI
{
    
    public string Type { get; } = nameof(StudyAreaMetricsMessage);
    public List<StudyAreasDataForUI> StudyAreaDataForUI { get; private set; }
    public StudyAreaMetricsMessage()
    {
        StudyAreaDataForUI = CoreModelSingelton.Instance.Studies
            .Select(x => RefreshActiveStudyAndMetrics(x))
            .ToList();
    }

    private static StudyAreasDataForUI RefreshActiveStudyAndMetrics(StudyArea study)
    {
        return new(
            study.Name,
            RoadLengthData: UpdateRoadLengthGraphData(study.Metrics),
            RoadAreaData: UpdateRoadAreaGraphData(study.Metrics),
            UrbanLandAreaData: UpdateLandUseGraphData(study.Metrics),
            UrbanBuiltAreaData: UpdateBuiltUseGraphData(study.Metrics),
            study.Metrics.BoundedArea,
            study.Metrics.BuiltArea,
            study.Metrics.FAR,
            study.Metrics.CoverageRatio);
    }

    private static List<GraphData> UpdateLandUseGraphData(StudyAreaMetrics metrics)
    {
        var graphData = new List<GraphData>();

        var metricsByUse = metrics.ParcelMetrics.GroupBy(p => p.Use);

        foreach (var group in metricsByUse)
        {
            graphData.Add(
                new GraphData(
                    Label: group.Key.Name,
                    Data: group.Sum(u => u.Area),
                    Color: group.Key.Color
                    ));
        }

        return graphData;
    }

    private static List<GraphData> UpdateBuiltUseGraphData(StudyAreaMetrics metrics)
    {
        var graphData = new List<GraphData>();

        var metricsByUse = metrics.ParcelMetrics.GroupBy(p => p.Use);

        foreach (var use in metricsByUse)
        {
            graphData.Add(
                new GraphData(
                    Label: use.Key.Name,
                    Data: use.Sum(u => u.BUA),
                    Color: use.Key.Color
                    ));
        }

        return graphData;
    }

    private static List<GraphData> UpdateRoadLengthGraphData(StudyAreaMetrics metrics)
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
                    Color: color
                    ));

        }

        return graphData;
    }

    private static List<GraphData> UpdateRoadAreaGraphData(StudyAreaMetrics metrics)
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
                    Color: color
                    ));
        }

        return graphData;
    }
}
