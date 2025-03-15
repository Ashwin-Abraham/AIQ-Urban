using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;

namespace ArchSitePlan.UI.HTML_UI.MessageToUI;
public class MetricsMessage :IMessageToUI
{
    public string Type { get; } = "MetricsMessage";
    public string StudyName { get; }
    public List<GraphData> RoadLengthData { get; private set; }
    public List<GraphData> RoadAreaData { get; private set; }
    public List<GraphData> UrbanLandAreaData { get; private set; }
    public List<GraphData> UrbanBuiltAreaData { get; private set; } 


    public MetricsMessage(string studyName)
    {
        StudyName = studyName;
        RefreshActiveStudyAndMetrics();
    }

    public void RefreshActiveStudyAndMetrics()
    {
        if (StudyName == string.Empty
            || !CoreModel.TryGetStudyArea(StudyName, out StudyArea? study))
            study = CoreModel.Studies.FirstOrDefault(s => s.Name == CoreGeneralConstants.FullProjectStudyName);

        if (study == null) return;

        UrbanLandAreaData = UpdateLandUseGraphData(study.Metrics);
        UrbanBuiltAreaData = UpdateBuiltUseGraphData(study.Metrics);
        RoadAreaData = UpdateRoadAreaGraphData(study.Metrics);
        RoadLengthData = UpdateRoadLengthGraphData(study.Metrics);
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

            var color = AppColours.StandardColors[colorIndex];
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

            var color = AppColours.StandardColors[colorIndex];
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
