using ArchSitePlan.Core.Factories.Models;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline;
using ArchSitePlan.Core.Pipeline.CreationData;
using Eto.Forms;
using Rhino;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.CoreModelInterface
{
    public record PipelineResult(bool Success, string Message);

    public static class ModelBuilderPipelines
    {
        public static void AddOrUpdateRoadPipeline(
            RhinoDoc doc,
            List<TransitData> creationData,
            ModellingTool currentAppMode)
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
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
            //LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        public static void ChangeParcelUsePipeline(
            RhinoDoc doc,
            List<Guid> parcelIds,
            Landuse newUse,
            ModellingTool currentAppMode)
        {
            if (!RhinoEventHandler.ValidLicense)
                return;

            CoreModelEditStepLibrary.UpdateParcelUse(parcelIds, newUse);
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
            //LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        public static void RefreshStudyAreaMetricsPipeline()
        {
            if (!RhinoEventHandler.ValidLicense)
                return;

            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
        }

        public static void AddOrUpdateParcelDividerPipeline(RhinoDoc doc, 
            List<DividerData> dividerData,
            ModellingTool currentAppMode)
        {
            if (!RhinoEventHandler.ValidLicense)
                return;

            CoreModelEditStepLibrary.TryRemoveDivders(dividerData);
            CoreModelEditStepLibrary.AddDivders(dividerData);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
            //LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
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
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
            //LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        internal static void AddOrUpdateBuldingPipeline(
            RhinoDoc doc,
            List<BuildingData> buildingData,
            ModellingTool currentAppMode)
        {
            if (!RhinoEventHandler.ValidLicense)
                return;

            CoreModelEditStepLibrary.AddOrUpdateBulding(buildingData);
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
            LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        internal static void AutoGenerateBuildingsOnParcel(
            RhinoDoc doc,
            List<BuildingLayout> layoutsToAdd,
            ModellingTool currentAppMode = ModellingTool.AutoBuilding)
        {
            CoreModelEditStepLibrary.RemoveBuildingsOnParcels(layoutsToAdd);
            var buildings = layoutsToAdd.SelectMany(x=> x.Buildings).ToList();
            AddOrUpdateBuldingPipeline(doc, buildings, currentAppMode);
        }

        internal static void AddOrUpdateBuldingPipeline(
            RhinoDoc doc,
            List<Building> buildingsToAdd,
            ModellingTool currentAppMode)
        {
            if (!RhinoEventHandler.ValidLicense)
                return;

            CoreModelEditStepLibrary.AddOrUpdateBulding(buildingsToAdd);
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
            //LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        internal static void DeleteBuildingPipeline(
            RhinoDoc doc, 
            List<BuildingData> buildingData,
            ModellingTool currentAppMode)
        {
            if (!RhinoEventHandler.ValidLicense)
                return;

            if (buildingData.Count == 0) return;
            CoreModelEditStepLibrary.TryDeleteBuildings(buildingData);
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
            //LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
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
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
            //LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
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
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
           // LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        internal static void UpdateStudyAreas(RhinoDoc doc, 
            List<StudyData> studyData,
            ModellingTool currentAppMode)
        {
            if (!RhinoEventHandler.ValidLicense)
                return;

            AddStudyAreaPipeline(doc, studyData, currentAppMode);
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
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
            LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
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
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
            //LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        internal static void DeleteParcelUseAndReassignUseBasedObjs(RhinoDoc doc, string oldUseName)
        {
            if (!AppLanduses.TryDeleteUse(oldUseName, out var deletedUse))
                return;

            CoreModelEditStepLibrary.ReplaceParcelUse(deletedUse!, AppLanduses.Undefined);
            CoreModelEditStepLibrary.ReplaceBuildingUse(deletedUse!, AppLanduses.Undefined);
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
        }
    }
}
