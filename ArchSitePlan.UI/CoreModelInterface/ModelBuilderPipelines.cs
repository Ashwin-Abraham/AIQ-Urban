using ArchSitePlan.Core.Pipeline;
using ArchSitePlan.Core.Pipeline.CreationData;
using Rhino;
using static ArchSitePlan.Core.Constants.Enums;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI.CoreModelInterface
{
    public static class ModelBuilderPipelines
    {

        public static void AddOrUpdateRoadPipeline(RhinoDoc doc,
            List<TransitData> creationData,
            AppMode currentAppMode)
        {
            CoreModelEditStepLibrary.TryRemoveRoad(creationData);
            CoreModelEditStepLibrary.AddOrUpdateRoad(creationData);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
            LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        public static void ChangeParcelUsePipeline(
            RhinoDoc doc,
            List<Guid> parcelIds,
            Landuse newUse,
            AppMode currentAppMode)
        {
            CoreModelEditStepLibrary.UpdateParcelUse(parcelIds, newUse);
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
            LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        public static void RefreshStudyAreaMetricsPipeline()
        {
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
        }

        public static void AddOrUpdateParcelDividerPipeline(RhinoDoc doc, 
            List<DividerData> dividerData,
            AppMode currentAppMode)
        {
            CoreModelEditStepLibrary.TryRemoveDivders(dividerData);
            CoreModelEditStepLibrary.AddDivders(dividerData);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
            LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }
        public static void DeleteParcelDividers(RhinoDoc doc, 
            List<DividerData> dividerData,
            AppMode currentAppMode)
        {
            if (!CoreModelEditStepLibrary.TryRemoveDivders(dividerData)) return;
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
            LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        internal static void AddOrUpdateBuldingPipeline(
            RhinoDoc doc,
            List<BuildingData> buildingData,
            AppMode currentAppMode)
        {
            CoreModelEditStepLibrary.AddOrUpdateBulding(buildingData);
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
            LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        internal static void DeleteBuildingPipeline(
            RhinoDoc doc, 
            List<BuildingData> buildingData,
            AppMode currentAppMode)
        {
            if (buildingData.Count == 0) return;
            CoreModelEditStepLibrary.TryDeleteBuildings(buildingData);
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
            LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }
        internal static void AddStudyAreaPipeline(RhinoDoc doc, 
            List<StudyData> studyData,
            AppMode currentAppMode)
        {
            CoreModelEditStepLibrary.AddOrUpdateStudyArea(studyData);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
            LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        internal static void DeleteStudyAreaPipeline(RhinoDoc doc, 
            List<Guid> guids,
            AppMode currentAppMode)
        {
            CoreModelEditStepLibrary.DeleteStudyAreas(guids);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
            LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }

        internal static void UpdateStudyAreas(RhinoDoc doc, 
            List<StudyData> studyData,
            AppMode currentAppMode)
        {
            AddStudyAreaPipeline(doc, studyData, currentAppMode);
        }

        internal static void ReloadAppPipeline(RhinoDoc doc, AppMode currentAppMode)
        {
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
            AppMode currentAppMode)
        {
            CoreModelEditStepLibrary.TryRemoveRoad(transitData);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
            LayerManager.ToggleLayerStateBasedOnAppMode(doc, currentAppMode);
        }
    }
}
