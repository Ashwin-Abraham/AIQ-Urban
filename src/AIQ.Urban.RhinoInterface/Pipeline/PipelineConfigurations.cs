using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models.Metrics;

namespace AIQ.Urban.RhinoInterface;

public static class PipelineConfigurations
{
    public static RhinoUrbanModelPipeline WithParcelBoundaryAdditionSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new AddParcelBoundaryStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithParcelBoundaryUpdateSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new UpdateParcelBoundaryStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithParcelBoundaryDeletionSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new RemoveParcelBoundaryStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithBuildingAddOrUpdateSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new AddBuildingStep(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new UpdateBuildingStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithTransitAdditionSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new RemoveTransitStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new AddTransitStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new TransitSurfaceUnioner(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithBuildingUseChangeSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new ChangeBuildingUseStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithUseChangeSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new ChangeParcelUseStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new ChangeBuildingUseStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithZoneAdditionSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new AddZoneStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithTransitDeletionSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new RemoveTransitStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new TransitSurfaceUnioner(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithTransitUpdateSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new UpdateTransitStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new TransitSurfaceUnioner(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithBuildingDeletionSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new RemoveBuildingStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithZoneDeletionSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new RemoveZoneStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithZoneUpdateSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new UpdateZoneStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithBuildingUpdateSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new UpdateBuildingStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithFloorPlateReplaceSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new ReplaceFloorPlateStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithMetricsCalculation(
        this RhinoUrbanModelPipeline pipeline,
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

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithLanduseListEditSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new ModifyModelUsesStep(pipeline.LoggerFactory));
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithDeleteAllUrbanElementsSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        AddRemovalSteps(pipeline);
        pipeline.WithUrbanElementStep(new TransitSurfaceUnioner(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new GraphCreationStep())
            .WithUrbanModelPostAction(model => model.ProjectMetrics = null);
        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithDocumentLoadSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        AddRemovalSteps(pipeline);
        pipeline.WithUrbanElementStep(new AddTransitStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new AddBuildingStep(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new AddZoneStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new AddParcelBoundaryStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new TransitSurfaceUnioner(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new GraphCreationStep())
            .WithFullModelOutput()
            .WithMetricsCalculation();
        return pipeline;
    }

    private static void AddRemovalSteps(RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new RemoveTransitStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new RemoveBuildingStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new RemoveZoneStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new RemoveParcelBoundaryStep(pipeline.LoggerFactory));
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
