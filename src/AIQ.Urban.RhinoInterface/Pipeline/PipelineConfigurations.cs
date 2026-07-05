using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.Metrics;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface;

public static class PipelineConfigurations
{
    public static RhinoUrbanModelPipeline WithBuildingAdditionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<BuildingData> buildingData,
        List<Guid> curvesToDelete)
    {
        var addBuildingStep = new AddBuildingStep(buildingData);

        pipeline.WithRhinoObjectDeletion(curvesToDelete)
            .WithUrbanElementStep(addBuildingStep);

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithTransitAdditionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<TransitData> transitData,
        List<Guid> centerlinesToRemove)
    {
        var addTransitStep = new AddTransitStep(transitData);
        var removeExistingTransitStep = new RemoveTransitStep(centerlinesToRemove);
        var transitUnioner = new TransitSurfaceUnioner();
        var recalculateParcelsStep = new RecalculateParcelsStep();

        pipeline.WithRhinoObjectDeletion(centerlinesToRemove)
            .WithUrbanElementStep(removeExistingTransitStep)
            .WithUrbanElementStep(addTransitStep)
            .WithUrbanElementStep(transitUnioner)
            .WithUrbanElementStep(recalculateParcelsStep);

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithBuildingUseChangeSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<Guid> geometryIds,
        Use newUse)
    {
        var changeStep = new ChangeBuildingUseStep(geometryIds, newUse);

        pipeline.WithUrbanElementStep(changeStep);

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithZoneAdditionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<ZoneData> zoneData,
        List<Guid> idsToDelete)
    {
        var addZoneStep = new AddZoneStep(zoneData);

        pipeline.WithRhinoObjectDeletion(idsToDelete)
            .WithUrbanElementStep(addZoneStep);

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithTransitDeletionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<Guid> centerlineIds)
    {
        var removeStep = new RemoveTransitStep(centerlineIds);
        var transitUnioner = new TransitSurfaceUnioner();
        var recalculateParcelsStep = new RecalculateParcelsStep();

        pipeline.WithUrbanElementStep(removeStep)
            .WithUrbanElementStep(transitUnioner)
            .WithUrbanElementStep(recalculateParcelsStep);

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithTransitUpdateSteps(
        this RhinoUrbanModelPipeline pipeline,
        Guid oldCenterlineId, 
        Curve newCenterline)
    {
        var updateStep = new UpdateTransitStep(oldCenterlineId, newCenterline);
        var transitUnioner = new TransitSurfaceUnioner();
        var recalculateParcelsStep = new RecalculateParcelsStep();

        pipeline.WithUrbanElementStep(updateStep)
            .WithUrbanElementStep(transitUnioner)
            .WithUrbanElementStep(recalculateParcelsStep);

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithBuildingDeletionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<Guid> geometryIds)
    {
        var removeStep = new RemoveBuildingStep(geometryIds);

        pipeline.WithUrbanElementStep(removeStep);

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithZoneDeletionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<Guid> geometryIds)
    {
        var removeStep = new RemoveZoneStep(geometryIds);

        pipeline.WithUrbanElementStep(removeStep);

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithZoneUpdateSteps(
        this RhinoUrbanModelPipeline pipeline,
        Guid oldBoundaryId, 
        Curve newBoundary)
    {
        var updateStep = new UpdateZoneStep(oldBoundaryId, newBoundary);

        pipeline.WithUrbanElementStep(updateStep);

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
