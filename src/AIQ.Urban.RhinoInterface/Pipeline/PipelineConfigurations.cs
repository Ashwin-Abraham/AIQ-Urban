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
        pipeline.WithRhinoObjectDeletion(curvesToDelete)
            .WithCreationData(buildingData)
            .WithUrbanElementStep(new AddBuildingStep(_loggerFactory: pipeline.LoggerFactory));

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithTransitAdditionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<TransitData> transitData,
        List<Guid> centerlinesToRemove)
    {
        pipeline.WithRhinoObjectDeletion(centerlinesToRemove)
            .WithCreationData(transitData.Cast<ICreationData>().ToList())
            .WithCreationData([new TransitDeletionData(centerlinesToRemove)])
            .WithUrbanElementStep(new RemoveTransitStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new AddTransitStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new TransitSurfaceUnioner(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory));

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithBuildingUseChangeSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<Guid> geometryIds,
        Use newUse)
    {
        pipeline.WithCreationData([new BuildingUseChangeData(geometryIds, newUse)])
            .WithUrbanElementStep(new ChangeBuildingUseStep(pipeline.LoggerFactory));

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithZoneAdditionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<ZoneData> zoneData,
        List<Guid> idsToDelete)
    {
        pipeline.WithRhinoObjectDeletion(idsToDelete)
            .WithCreationData(zoneData.Cast<ICreationData>().ToList())
            .WithCreationData([new ZoneDeletionData(idsToDelete)])
            .WithUrbanElementStep(new AddZoneStep(pipeline.LoggerFactory));

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithTransitDeletionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<Guid> centerlineIds)
    {
        pipeline.WithCreationData([new TransitDeletionData(centerlineIds)])
            .WithUrbanElementStep(new RemoveTransitStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new TransitSurfaceUnioner(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory));

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithTransitUpdateSteps(
        this RhinoUrbanModelPipeline pipeline,
        Guid oldCenterlineId, 
        Curve newCenterline)
    {
        pipeline.WithCreationData([new TransitUpdateData(oldCenterlineId, newCenterline)])
            .WithUrbanElementStep(new UpdateTransitStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new TransitSurfaceUnioner(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory));

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithBuildingDeletionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<Guid> geometryIds)
    {
        pipeline.WithCreationData([new BuildingDeletionData(geometryIds)])
            .WithUrbanElementStep(new RemoveBuildingStep(pipeline.LoggerFactory));

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithZoneDeletionSteps(
        this RhinoUrbanModelPipeline pipeline,
        List<Guid> geometryIds)
    {
        pipeline.WithCreationData([new ZoneDeletionData(geometryIds)])
            .WithUrbanElementStep(new RemoveZoneStep(pipeline.LoggerFactory));

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithZoneUpdateSteps(
        this RhinoUrbanModelPipeline pipeline,
        Guid oldBoundaryId, 
        Curve newBoundary)
    {
        pipeline.WithCreationData([new ZoneUpdateData(oldBoundaryId, newBoundary)])
            .WithUrbanElementStep(new UpdateZoneStep(pipeline.LoggerFactory));

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithBuildingUpdateSteps(
        this RhinoUrbanModelPipeline pipeline,
        Guid oldEnvelopeId,
        Brep newEnvelope,
        double typicalFloorHeight,
        List<Use> oldPlateUses)
    {
        pipeline.WithCreationData([new BuildingUpdateData(oldEnvelopeId, newEnvelope, typicalFloorHeight, oldPlateUses)])
            .WithUrbanElementStep(new UpdateBuildingStep(pipeline.LoggerFactory));

        return pipeline;
    }

    public static RhinoUrbanModelPipeline WithFloorPlateReplaceSteps(
        this RhinoUrbanModelPipeline pipeline,
        FloorPlate oldPlate,
        double newZ)
    {
        pipeline.WithCreationData([new FloorPlateReplaceData(oldPlate, newZ)])
            .WithUrbanElementStep(new ReplaceFloorPlateStep(pipeline.LoggerFactory));

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
        pipeline.WithCreationData([new UseModificationData(editType, name, color, id)])
            .WithUrbanElementStep(new ModifyModelUsesStep(pipeline.LoggerFactory));

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
        var aiqLayerObjectIds = doc.Objects
            .Where(x => allLayerIndices.Contains(x.Attributes.LayerIndex))
            .Select(x => x.Id)
            .ToList();

        var gatherStep = new DocLoadGatherStep(model, doc.ModelAbsoluteTolerance, pipeline.LoggerFactory);

        pipeline.WithDataGatherStep(gatherStep)
            .WithRhinoObjectDeletion(aiqLayerObjectIds)
            .WithUrbanElementStep(new ResetUrbanElementsStep())
            .WithUrbanElementStep(new AddTransitStep(pipeline.LoggerFactory))
            .WithUrbanElementStep(new AddBuildingStep(_loggerFactory: pipeline.LoggerFactory))
            .WithUrbanElementStep(new AddZoneStep(pipeline.LoggerFactory))
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
