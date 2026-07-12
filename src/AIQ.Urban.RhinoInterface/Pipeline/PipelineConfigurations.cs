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

        model.Reset();

        var transitPrefix = LayerConstants.Transit + "::";
        var centerlines = RhinoDocScanner.FindCurvesOnLayerPrefix(doc, transitPrefix)
            .Where(c => c.LayerPath.Contains("::Centerline::"))
            .ToList();

        foreach (var (curve, objectId, layerPath) in centerlines)
        {
            var parsed = LayerNameParser.ParseTransitLayer(layerPath);
            if (parsed is null) continue;

            var curve2d = new Curve2D(curve, objectId);
            var transit = new Transit(curve2d, parsed.Value.Width, parsed.Value.Type);
            model.AddTransit([transit]);
        }

        var parcelPrefix = LayerConstants.Parcel + "::";
        var parcelBreps = RhinoDocScanner.FindBrepsOnLayerPrefix(doc, parcelPrefix);
        var docParcelCentroids = new List<(Point3d Centroid, Use Use)>();
        foreach (var (brep, objectId, layerPath) in parcelBreps)
        {
            var useName = LayerNameParser.ParseParcelUse(layerPath);
            if (useName is null) continue;
            Use use;
            try { use = model.GetUse(useName); }
            catch { use = model.GetUse("Undefined"); }

            var centroid = AreaMassProperties.Compute(brep)?.Centroid;
            if (centroid.HasValue)
                docParcelCentroids.Add((centroid.Value, use));
        }

        var transitUnioner = new TransitSurfaceUnioner();
        var recalcParcels = new RecalculateParcelsStep();
        if (docParcelCentroids.Count > 0)
            recalcParcels.ExternalCentroids = docParcelCentroids;

        pipeline.WithUrbanElementStep(transitUnioner)
            .WithUrbanElementStep(recalcParcels);

        var envelopeBreps = RhinoDocScanner.FindBrepsOnLayerPrefix(doc, LayerConstants.BuildingEnvelope);
        var floorPlateCurves = RhinoDocScanner.FindCurvesOnLayerPrefix(doc, LayerConstants.BuildingFloorPlates + "::");

        foreach (var (envelopeBrep, envelopeId, _) in envelopeBreps)
        {
            var associatedPlates = new List<FloorPlate>();
            foreach (var (fpCurve, fpId, fpLayerPath) in floorPlateCurves)
            {
                var useName = LayerNameParser.ParseFloorPlateUse(fpLayerPath);
                if (useName is null) continue;
                Use use;
                try { use = model.GetUse(useName); }
                catch { use = model.GetUse("Undefined"); }

                var fpCentroid = AreaMassProperties.Compute(fpCurve)?.Centroid;
                if (fpCentroid is null) continue;

                var isInside = envelopeBrep.IsPointInside(fpCentroid.Value, doc.ModelAbsoluteTolerance, true);
                if (isInside)
                {
                    var polygon = new Polygon2D(fpCurve);
                    associatedPlates.Add(new FloorPlate(polygon, use));
                }
            }

            if (associatedPlates.Count == 0) continue;

            var building = new Building(envelopeBrep, associatedPlates);
            model.AddBuilding(building);
        }

        var zonePrefix = LayerConstants.ZoneRoot + "::";
        var zoneCurves = RhinoDocScanner.FindCurvesOnLayerPrefix(doc, zonePrefix);
        foreach (var (curve, objectId, layerPath) in zoneCurves)
        {
            var zoneName = LayerNameParser.ParseZoneName(layerPath);
            if (zoneName is null) continue;

            var polygon = new Polygon2D(curve) { Id = objectId };
            var zone = new Zone(polygon, zoneName);
            model.AddZone(zone);
        }

        pipeline.WithMetricsCalculation();

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
