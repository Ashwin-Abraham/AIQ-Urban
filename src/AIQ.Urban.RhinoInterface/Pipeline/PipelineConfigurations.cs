using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.RhinoInterface.Pipeline.Configs;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface;

public class PipelineConfigurations(RhinoUrbanModelPipeline pipeline)
{
    public PipelineConfigurations WithBuildingAdditionSteps(BuildingPipelineConfiguration config)
    {
        var addBuildingStep = new AddBuildingStep(config.BuildingData);

        pipeline.WithRhinoObjectDeletion(config.InputCurveIds)
            .WithUrbanElementStep(addBuildingStep);

        return this;
    }

    public PipelineConfigurations WithTransitAdditionSteps(TransitPipelineConfiguration config)
    {
        var addTransitStep = new AddTransitStep(config.TransitData);
        var removeExistingTransitStep = new RemoveTransitStep(config.CenterlineIds);
        var transitUnioner = new TransitSurfaceUnioner();
        var recalculateParcelsStep = new RecalculateParcelsStep();

        pipeline.WithRhinoObjectDeletion(config.CenterlineIds)
            .WithUrbanElementStep(removeExistingTransitStep)
            .WithUrbanElementStep(addTransitStep)
            .WithUrbanElementStep(transitUnioner)
            .WithUrbanElementStep(recalculateParcelsStep);

        return this;
    }

    public PipelineConfigurations WithBuildingUseChangeSteps(List<Guid> geometryIds, Use newUse)
    {
        var changeStep = new ChangeBuildingUseStep(geometryIds, newUse);

        pipeline.WithUrbanElementStep(changeStep);

        return this;
    }

    public PipelineConfigurations WithZoneAdditionSteps(ZonePipelineConfiguration config)
    {
        var addZoneStep = new AddZoneStep(config.ZoneData);

        pipeline.WithRhinoObjectDeletion(config.InputCurveIds)
            .WithUrbanElementStep(addZoneStep);

        return this;
    }

    public PipelineConfigurations WithTransitDeletionSteps(List<Guid> centerlineIds)
    {
        var removeStep = new RemoveTransitStep(centerlineIds);
        var transitUnioner = new TransitSurfaceUnioner();
        var recalculateParcelsStep = new RecalculateParcelsStep();

        pipeline.WithUrbanElementStep(removeStep)
            .WithUrbanElementStep(transitUnioner)
            .WithUrbanElementStep(recalculateParcelsStep);

        return this;
    }

    public PipelineConfigurations WithTransitUpdateSteps(Guid oldCenterlineId, Curve newCenterline)
    {
        var updateStep = new UpdateTransitStep(oldCenterlineId, newCenterline);
        var transitUnioner = new TransitSurfaceUnioner();
        var recalculateParcelsStep = new RecalculateParcelsStep();

        pipeline.WithUrbanElementStep(updateStep)
            .WithUrbanElementStep(transitUnioner)
            .WithUrbanElementStep(recalculateParcelsStep);

        return this;
    }

    public PipelineConfigurations WithBuildingDeletionSteps(List<Guid> geometryIds)
    {
        var removeStep = new RemoveBuildingStep(geometryIds);

        pipeline.WithUrbanElementStep(removeStep);

        return this;
    }

    public PipelineConfigurations WithZoneDeletionSteps(List<Guid> geometryIds)
    {
        var removeStep = new RemoveZoneStep(geometryIds);

        pipeline.WithUrbanElementStep(removeStep);

        return this;
    }

    public PipelineConfigurations WithZoneUpdateSteps(Guid oldBoundaryId, Curve newBoundary)
    {
        var updateStep = new UpdateZoneStep(oldBoundaryId, newBoundary);

        pipeline.WithUrbanElementStep(updateStep);

        return this;
    }

    public PipelineConfigurations WithMetricsCalculation(
        bool saveToFile = false,
        double tolerance = 0.001,
        string? docName = null)
    {
        pipeline.WithUrbanModelPostAction(um => ProjectMetricsCalculator.CalculateAndStore(um, tolerance));

        if (saveToFile)
        {
            var path = MakeMetricsFilePath(docName);
            pipeline.WithUrbanModelPostAction(um => ProjectMetricsJsonSerializer.Save(um.ProjectMetrics!, path));
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
