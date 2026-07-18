using System.Drawing;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface;

public static class PipelineConfigurations
{
    public static RhinoUrbanModelPipeline WithBuildingAdditionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<ICreationData> buildingData,
        List<Guid> curvesToDelete)
    {
        var addBuildingStep = new AddBuildingStep();

        pipeline.WithRhinoObjectDeletion(curvesToDelete)
            .WithCreationData(buildingData)
            .WithUrbanElementStep(addBuildingStep);

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithTransitAdditionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<TransitData> transitData,
        List<Guid> centerlinesToRemove)
    {
        var addTransitStep = new AddTransitStep();
        var removeExistingTransitStep = new RemoveTransitStep(centerlinesToRemove);
        var transitUnioner = new TransitSurfaceUnioner();
        var recalculateParcelsStep = new RecalculateParcelsStep();

        pipeline.WithRhinoObjectDeletion(centerlinesToRemove)
            .WithCreationData(transitData.Cast<ICreationData>().ToList())
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
        var addZoneStep = new AddZoneStep();

        pipeline.WithRhinoObjectDeletion(idsToDelete)
            .WithCreationData(zoneData.Cast<ICreationData>().ToList())
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

    public static RhinoUrbanModelPipeline WithBuildingUpdateSteps(
        this RhinoUrbanModelPipeline pipeline,
        Guid oldEnvelopeId,
        Brep newEnvelope,
        double typicalFloorHeight,
        List<Use> oldPlateUses)
    {
        var updateStep = new UpdateBuildingStep(oldEnvelopeId, newEnvelope, typicalFloorHeight, oldPlateUses);

        pipeline.WithUrbanElementStep(updateStep);

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithFloorPlateReplaceSteps(
        this RhinoUrbanModelPipeline pipeline,
        FloorPlate oldPlate,
        double newZ)
    {
        var replaceStep = new ReplaceFloorPlateStep(oldPlate, newZ);

        pipeline.WithUrbanElementStep(replaceStep);

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
        this RhinoUrbanModelPipeline pipeline,
        UseEditType editType,
        string name,
        Color color,
        Guid id)
    {
        var modifyStep = new ModifyModelUsesStep(editType, name, color, id);
        pipeline.WithUrbanElementStep(modifyStep);

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithDocModelLoad(
        this RhinoUrbanModelPipeline pipeline,
        RhinoDoc doc)
    {
        var model = pipeline.UrbanModel;

        // Construct direct delete object ids
        var rootIndex = doc.Layers.FindByFullPath(LayerConstants.Root, -1);
        if (rootIndex == -1)
            return pipeline;

        var aiqRootLayer = doc.Layers[rootIndex];
        var allLayerIndices = aiqRootLayer.GetChildren(true).Append(aiqRootLayer).Select(x => x.Index).ToList();
        var aiqLayerObjectIds = doc.Objects
            .Where(x => allLayerIndices.Contains(x.Attributes.LayerIndex))
            .Select(x => x.Id)
            .ToList();

        // Construct creation data gather step
        var gatherStep = new DocLoadGatherStep(model, doc.ModelAbsoluteTolerance);

        // Compose pipeline
        pipeline.WithDataGatherStep(gatherStep)
            .WithRhinoObjectDeletion(aiqLayerObjectIds)
            .WithUrbanElementStep(new ResetUrbanElementsStep())
            .WithUrbanElementStep(new AddTransitStep())
            .WithUrbanElementStep(new AddBuildingStep())
            .WithUrbanElementStep(new AddZoneStep())
            .WithUrbanElementStep(new TransitSurfaceUnioner())
            .WithUrbanElementStep(new RecalculateParcelsStep())
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
