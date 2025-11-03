using ArchSitePlan.Core.Factories.Models;
using ArchSitePlan.Core.ModelEditActions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline;
using ArchSitePlan.Core.Pipeline.CreationData;
using Eto.Forms;
using Rhino;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.CoreModelInterface;

public static class ModelBuilderPipelines
{
    public static void AddOrUpdateRoadPipeline(
        List<TransitData> creationData,
        bool updateRhino = false)
    {
        if (!RhinoEventHandler.ValidLicense)
        {
            MessageBox.Show("Invalid license");
            return;
        }

        CoreModelEditStepLibrary.TryRemoveRoad(creationData);
        CoreModelEditStepLibrary.AddOrUpdateRoad(creationData);
        CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
        CoreModelEditStepLibrary.DivideParcelsAndTransit();
        CoreModelEditStepLibrary.EnsureEachFloorplateHasUse();

        if(updateRhino)
            RhinoOutputPipeline(RhinoDoc.ActiveDoc, ModellingTool.Transit, true);
    }

    public static void ChangeObjectUsePipeline(
        List<Guid> objectIds,
        string newUse)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        // Change buildings
        var buildings = CoreModelSingelton.Instance.Buildings
            .Where(x => objectIds.Contains(x.EnvelopeId))
            .ToList();

        var floorplatesInBuilding = buildings.SelectMany(x => x.FloorPlates).ToList();

        // Change floorplates
        var floorplateIds = CoreModelSingelton.Instance.Buildings.SelectMany(x => x.FloorPlates).Select(x => x.Id).ToHashSet();
        var floorplateIdsForPipeline = objectIds
            .Where(id => floorplateIds.Contains(id))
            .Concat(floorplatesInBuilding.Select(x => x.Id))
            .ToList();

        CoreModelEditStepLibrary.UpdateFloorplateUse(floorplateIdsForPipeline, newUse);

        // Change parcels
        var parcelIdsForPipeline = objectIds
            .Where(id => CoreModelSingelton.Instance.Parcels.Any(b => b.ClientModelId == id))
            .ToList();
        ChangeParcelUsePipeline(parcelIdsForPipeline, newUse);

        CoreModelEditStepLibrary.UpdateParcelUse(objectIds, newUse);
        CoreModelEditStepLibrary.EnsureEachFloorplateHasUse();
    }

    private static void ChangeParcelUsePipeline(
        List<Guid> parcelIds,
        string newUse)
    {
        CoreModelEditStepLibrary.UpdateParcelUse(parcelIds, newUse);
        CoreModelEditStepLibrary.EnsureEachFloorplateHasUse();
    }

    public static void AddOrUpdateParcelDividerPipeline(
        List<DividerData> dividerData,
        bool updateRhino = false)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        CoreModelEditStepLibrary.TryRemoveDivders(dividerData);
        CoreModelEditStepLibrary.AddDivders(dividerData);
        CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
        CoreModelEditStepLibrary.DivideParcelsAndTransit();
        CoreModelEditStepLibrary.EnsureEachFloorplateHasUse();

        if(updateRhino)
            RhinoOutputPipeline(RhinoDoc.ActiveDoc, ModellingTool.Dividers, true);
    }

    public static void DeleteParcelDividers(RhinoDoc doc, 
        List<DividerData> dividerData,
        ModellingTool currentAppMode,
        bool updateRhino = false)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        if (!CoreModelEditStepLibrary.TryRemoveDivders(dividerData)) return;
        CoreModelEditStepLibrary.DivideParcelsAndTransit();
        CoreModelEditStepLibrary.EnsureEachFloorplateHasUse();

        if(updateRhino)
            RhinoOutputPipeline(doc, currentAppMode, true);
    }

    internal static void AddOrUpdateBuldingPipeline(
        List<BuildingData> buildingData,
        ModellingTool appMode,
        bool triggerRhinoUpdate = false)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        CoreModelEditStepLibrary.AddOrUpdateBulding(buildingData);
        CoreModelEditStepLibrary.EnsureEachFloorplateHasUse();
        CoreModelEditStepLibrary.RefreshStudyAreaMetrics();

        if(triggerRhinoUpdate)
            RhinoOutputPipeline(RhinoDoc.ActiveDoc, appMode, true);
    }

    internal static void AutoGenerateBuildingsOnParcel(
        List<BuildingLayout> layoutsToAdd,
        bool updateRhino = false)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        CoreModelEditStepLibrary.RemoveBuildingsOnParcels(layoutsToAdd);

        var buildings = layoutsToAdd.SelectMany(x=> x.Buildings).ToList();
        CoreModelEditStepLibrary.AddOrUpdateBulding(buildings);
        CoreModelEditStepLibrary.EnsureEachFloorplateHasUse();

        if(updateRhino)
            RhinoOutputPipeline(RhinoDoc.ActiveDoc, RhinoEventHandler.HTMLViewModel.AppMode, true);
    }

    internal static void DeleteBuildingPipeline(
        RhinoDoc doc, 
        List<BuildingData> buildingData,
        ModellingTool currentAppMode,
        bool triggerRhinoUpdate = false)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        if (buildingData.Count == 0) return;
        CoreModelEditStepLibrary.TryDeleteBuildings(buildingData);

        if(triggerRhinoUpdate)
            RhinoOutputPipeline(doc, currentAppMode, true);
    }

    internal static void AddStudyAreaPipeline(
        RhinoDoc doc, 
        List<StudyData> studyData,
        ModellingTool currentAppMode,
        bool triggerRhinoUpdate = false)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        CoreModelEditStepLibrary.AddOrUpdateStudyArea(studyData);
        CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
        CoreModelEditStepLibrary.DivideParcelsAndTransit();
        CoreModelEditStepLibrary.EnsureEachFloorplateHasUse();

        if(triggerRhinoUpdate)
            RhinoOutputPipeline(doc, currentAppMode, true);
    }

    internal static void DeleteStudyAreaPipeline(RhinoDoc doc, 
        List<Guid> guids,
        ModellingTool currentAppMode)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        CoreModelEditStepLibrary.DeleteStudyAreas(guids);
        CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
        CoreModelEditStepLibrary.DivideParcelsAndTransit();
        CoreModelEditStepLibrary.EnsureEachFloorplateHasUse();
        RhinoOutputPipeline(doc, currentAppMode, true);
    }

    internal static void UpdateStudyAreas(RhinoDoc doc, 
        List<StudyData> studyData,
        ModellingTool currentAppMode,
        bool updateRhino = false)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        AddStudyAreaPipeline(doc, studyData, currentAppMode);

        if(updateRhino)
            RhinoOutputPipeline(doc, currentAppMode, true);
    }

    public static void ReloadAppPipeline(RhinoDoc doc, ModellingTool currentAppMode)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        RhinoEventHandler.SubscribeModelEvents();

        // Extract required data
        var landuseData = RhinoModelEditStepLibrary.ExtractRhinoLanduseData(doc, out var landuseAtPointsData);
        var transitData = RhinoModelEditStepLibrary.ExtractTransitFromRhino(doc);
        var studyData = RhinoModelEditStepLibrary.ExtractStudyAreasFromRhino(doc);
        var buildingData = RhinoModelEditStepLibrary.ExtractBuildingCreationDataFromRhino(doc);
        var dividerData = RhinoModelEditStepLibrary.ExtractDividerData(doc);

        // Edit Core Model
        CoreModelEditStepLibrary.ClearCoreModel();
        CoreModelEditStepLibrary.SetLanduses(landuseData);
        CoreModelEditStepLibrary.AddOrUpdateRoad(transitData);
        CoreModelEditStepLibrary.AddOrUpdateStudyArea(studyData);
        CoreModelEditStepLibrary.AddDivders(dividerData);
        CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
        CoreModelEditStepLibrary.DivideParcelsAndTransit();
        CoreModelEditStepLibrary.EditParcelUsesByClosestPoint(landuseAtPointsData);
        CoreModelEditStepLibrary.AddOrUpdateBulding(buildingData);

        //Output
        RhinoOutputPipeline(doc, currentAppMode, true);
    }

    public static void RhinoOutputPipeline(
        RhinoDoc doc, 
        ModellingTool currentAppMode,
        bool regenerateBuildings,
        bool withHistoryUpdate = true)
    {
        //Output
        RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, regenerateBuildings);
        LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        CoreModelEditStepLibrary.RefreshStudyAreaMetrics();

        return;

        // Store history, unless this was called as part of an undo/redo action
        // In that case the state being generated is already in history
        if (withHistoryUpdate)
            CoreModelEditStepLibrary.StoreHistory();
    }


    internal static void RemoveRoadPipeline(List<TransitData> transitData, 
        RhinoDoc doc,
        ModellingTool currentAppMode)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        CoreModelEditStepLibrary.TryRemoveRoad(transitData);
        CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
        CoreModelEditStepLibrary.DivideParcelsAndTransit();
    }

    internal static void TryRollBack()
    {
        return;

        if (!HistoryAction.Undo())
            return;

        RhinoOutputPipeline(
            RhinoDoc.ActiveDoc,
            RhinoEventHandler.HTMLViewModel.AppMode,
            true,
            false);
    }

    internal static void TryRollForward()
    {
        return;

        if (HistoryAction.Redo())
            return;

        RhinoOutputPipeline(
            RhinoDoc.ActiveDoc,
            RhinoEventHandler.HTMLViewModel.AppMode,
            true,
            false);
    }
}
