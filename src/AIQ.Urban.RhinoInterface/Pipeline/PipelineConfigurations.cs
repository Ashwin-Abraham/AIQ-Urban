using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.RhinoInterface.Pipeline.Configs;

namespace AIQ.Urban.RhinoInterface;

public class PipelineConfigurations(RhinoUrbanModelPipeline pipeline)
{
    public PipelineConfigurations WithBuildingAdditionSteps(BuildingPipelineConfiguration config)
    {
        var addBuildingStep = new AddBuildingStep(config.BuildingData);

        pipeline.WithRhinoObjectDeletion(config.InputCurveIds)
            .WithUrbanElementAddition(addBuildingStep);

        return this;
    }

    public PipelineConfigurations WithTransitAdditionSteps(TransitPipelineConfiguration config)
    {
        var addTransitStep = new AddTransitStep(config.TransitData);
        var removeExistingTransitStep = new RemoveTransitStep(config.CenterlineIds);
        var transitUnioner = new TransitSurfaceUnioner();
        var recalculateParcelsStep = new RecalculateParcelsStep();

        pipeline.WithRhinoObjectDeletion(config.CenterlineIds)
            .WithUrbanElementDeletion(removeExistingTransitStep)
            .WithUrbanElementAddition(addTransitStep)
            .WithUrbanElementAddition(transitUnioner)
            .WithUrbanElementAddition(recalculateParcelsStep);

        return this;
    }

    public PipelineConfigurations WithZoneAdditionSteps(ZonePipelineConfiguration config)
    {
        var addZoneStep = new AddZoneStep(config.ZoneData);

        pipeline.WithRhinoObjectDeletion(config.InputCurveIds)
            .WithUrbanElementAddition(addZoneStep);

        return this;
    }

    public PipelineConfigurations WithMetricsCalculation(
        bool saveToFile = false,
        double tolerance = 0.001,
        string? docName = null)
    {
        pipeline.WithUrbanModelAction(um => ProjectMetricsCalculator.CalculateAndStore(um, tolerance));

        if (saveToFile)
        {
            var path = MakeMetricsFilePath(docName);
            pipeline.WithUrbanModelAction(um => ProjectMetricsJsonSerializer.Save(um.ProjectMetrics!, path));
        }

        return this;
    }

    private static string MakeMetricsFilePath(string? docName)
    {
        var name = string.IsNullOrEmpty(docName)
            ? "Untitled"
            : Path.GetFileNameWithoutExtension(docName);
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "AIQ Urban");
        return Path.Combine(root, $"{name}-project-metrics.json");
    }
}
