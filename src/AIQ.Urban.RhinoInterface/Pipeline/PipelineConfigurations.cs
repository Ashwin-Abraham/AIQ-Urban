using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;
using Rhino.Geometry;

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

    public static RhinoUrbanModelPipeline WithTransitRestoreSteps(
        this RhinoUrbanModelPipeline pipeline)
    {
        pipeline.WithUrbanElementStep(new RestoreTransitStep(pipeline.LoggerFactory));

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

    public static RhinoUrbanModelPipeline WithDocModelLoad(
        this RhinoUrbanModelPipeline pipeline,
        RhinoDoc doc)
    {
        var model = pipeline.UrbanModel;

        var rootIndex = doc.Layers.FindByFullPath(LayerConstants.Root, -1);
        if (rootIndex == -1)
            return pipeline;

        var aiqRootLayer = doc.Layers[rootIndex];
        var allLayerIndices = aiqRootLayer.GetChildren(true).Append(aiqRootLayer).Select(x => x.Index).ToList();
        var gatherStep = new DocLoadGatherStep(model, doc.ModelAbsoluteTolerance, pipeline.LoggerFactory);
        var deletionGatherStep = new ManagedRhinoObjectsDeletionGatherStep(allLayerIndices);

        pipeline.WithDataGatherStep(gatherStep)
            .WithDataGatherStep(deletionGatherStep)
            .WithUrbanElementStep(new ResetUrbanElementsStep())
            .WithUrbanElementStep(new AddTransitStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new AddBuildingStep(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new AddZoneStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new AddParcelBoundaryStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new TransitSurfaceUnioner(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory))
            .WithFullModelOutput()
            .WithMetricsCalculation();

        return pipeline;
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
