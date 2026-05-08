using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Utils;

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

    public ModelStateMessage(List<StudyAreaMetrics> studyMetrics)
    {
        StudyAreaDataForUI = studyMetrics
            .Select(x => ConvertToStudyForUI(x))
            .ToList();

        Landuses = CoreModelSingelton.Instance.Landuses;
    }

    private static StudyAreasDataForUI ConvertToStudyForUI(StudyAreaMetrics metrics)
    {
        return new(
            metrics.ZoneName,
            RoadLengthData: metrics.ExtractRoadLengthGraphData(),
            RoadAreaData: metrics.ExtractRoadAreaGraphData(),
            UrbanLandAreaData: metrics.ExtractLandAreaGraphData(),
            UrbanBuiltAreaData: metrics.ExtractRoadLengthGraphData(),
            metrics.DevelopableArea,
            metrics.BuiltArea,
            metrics.FAR,
            metrics.CoverageRatio);
    }
}
