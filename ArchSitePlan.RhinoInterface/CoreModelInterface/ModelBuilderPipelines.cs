using ArchSitePlan.Core.Factories.Models;
using ArchSitePlan.Core.ModelEditActions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline;
using ArchSitePlan.Core.Pipeline.CreationData;
using Eto.Forms;
using Rhino;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.CoreModelInterface;

public record PipelineResult(bool Success, string Message);

public static class ModelBuilderPipelines
{
    public static void AddOrUpdateRoadPipeline(List<TransitData> creationData)
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
        CoreModelEditStepLibrary.RefreshBuildingUse();
        RhinoOutputPipeline(RhinoDoc.ActiveDoc, ModellingTool.Transit, true);
    }

    public static void ChangeParcelUsePipeline(List<Guid> parcelIds, Landuse newUse)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        CoreModelEditStepLibrary.UpdateParcelUse(parcelIds, newUse);
        CoreModelEditStepLibrary.RefreshBuildingUse();
    }

    public static void RefreshStudyAreaMetricsPipeline()
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        CoreModelEditStepLibrary.DivideParcelsAndTransit();
        CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
    }

    public static void AddOrUpdateParcelDividerPipeline(List<DividerData> dividerData)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        CoreModelEditStepLibrary.TryRemoveDivders(dividerData);
        CoreModelEditStepLibrary.AddDivders(dividerData);
        CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
        CoreModelEditStepLibrary.DivideParcelsAndTransit();
        CoreModelEditStepLibrary.RefreshBuildingUse();
        RhinoOutputPipeline(RhinoDoc.ActiveDoc, ModellingTool.Dividers, true);
    }
    public static void DeleteParcelDividers(RhinoDoc doc, 
        List<DividerData> dividerData,
        ModellingTool currentAppMode)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        if (!CoreModelEditStepLibrary.TryRemoveDivders(dividerData)) return;
        CoreModelEditStepLibrary.DivideParcelsAndTransit();
        CoreModelEditStepLibrary.RefreshBuildingUse();

        RhinoOutputPipeline(doc, currentAppMode, true);
    }

    internal static void AddOrUpdateBuldingPipeline(List<BuildingData> buildingData)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        CoreModelEditStepLibrary.AddOrUpdateBulding(buildingData);
        CoreModelEditStepLibrary.RefreshBuildingUse();
        CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
        RhinoOutputPipeline(RhinoDoc.ActiveDoc, ModellingTool.Building, true);
    }

    internal static void AutoGenerateBuildingsOnParcel( List<BuildingLayout> layoutsToAdd)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        CoreModelEditStepLibrary.RemoveBuildingsOnParcels(layoutsToAdd);

        var buildings = layoutsToAdd.SelectMany(x=> x.Buildings).ToList();
        CoreModelEditStepLibrary.AddOrUpdateBulding(buildings);
        CoreModelEditStepLibrary.RefreshBuildingUse();
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

    internal static void AddStudyAreaPipeline(RhinoDoc doc, 
        List<StudyData> studyData,
        ModellingTool currentAppMode)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        CoreModelEditStepLibrary.AddOrUpdateStudyArea(studyData);
        CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
        CoreModelEditStepLibrary.DivideParcelsAndTransit();
        CoreModelEditStepLibrary.RefreshBuildingUse();
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
        CoreModelEditStepLibrary.RefreshBuildingUse();
        RhinoOutputPipeline(doc, currentAppMode, true);
    }

    internal static void UpdateStudyAreas(RhinoDoc doc, 
        List<StudyData> studyData,
        ModellingTool currentAppMode)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        AddStudyAreaPipeline(doc, studyData, currentAppMode);
        RhinoOutputPipeline(doc, currentAppMode, true);
    }

    public static void ReloadAppPipeline(RhinoDoc doc, ModellingTool currentAppMode)
    {
        if (!RhinoEventHandler.ValidLicense)
            return;

        RhinoEventHandler.SubscribeModelEvents();

        // Extract required data
        var landuseData = RhinoModelEditStepLibrary.ExtractRhinoLanduseData(doc);
        var transitData = RhinoModelEditStepLibrary.ExtractTransitFromRhino(doc);
        var studyData = RhinoModelEditStepLibrary.ExtractStudyAreasFromRhino(doc);
        var buildingData = RhinoModelEditStepLibrary.ExtractBuildingCreationDataFromRhino(doc);
        var dividerData = RhinoModelEditStepLibrary.ExtractDividerData(doc);

        // Edit Core Model
        CoreModelEditStepLibrary.ClearCoreModel();
        CoreModelEditStepLibrary.AddOrUpdateRoad(transitData);
        CoreModelEditStepLibrary.AddOrUpdateStudyArea(studyData);
        CoreModelEditStepLibrary.AddOrUpdateBulding(buildingData);
        CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
        CoreModelEditStepLibrary.EditParcelUsesByClosestPoint(landuseData);
        CoreModelEditStepLibrary.AddDivders(dividerData);
        CoreModelEditStepLibrary.DivideParcelsAndTransit();

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

        // Store history, unless this was called as part of an undo/redo action
        // In that case the state being generated is already in history
        if (withHistoryUpdate && false)
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
        RhinoOutputPipeline(doc, currentAppMode, true);
    }

    internal static void DeleteParcelUseAndReassignUseBasedObjs(RhinoDoc doc, string oldUseName)
    {
        if (!AppLanduses.TryDeleteUse(oldUseName, out var deletedUse))
            return;

        CoreModelEditStepLibrary.ReplaceParcelUse(deletedUse!, AppLanduses.Undefined);
        CoreModelEditStepLibrary.ReplaceBuildingUse(deletedUse!, AppLanduses.Undefined);
        RhinoOutputPipeline(doc, ModellingTool.Zones, true);
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
