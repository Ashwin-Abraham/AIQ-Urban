using ArchSitePlan.Core.Pipeline;
using ArchSitePlan.Core.Pipeline.CreationData;
using Rhino;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.UI.CoreModelInterface
{
    public static class ModelBuilderPipelines
    {

        public static void AddOrUpdateRoadPipeline(RhinoDoc doc,
            List<TransitData> creationData)
        {
            CoreModelEditStepLibrary.TryRemoveRoad(creationData);
            CoreModelEditStepLibrary.AddOrUpdateRoad(creationData);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
        }

        public static void ChangeParcelUsePipeline(
            RhinoDoc doc,
            List<Guid> parcelIds,
            Landuse newUse)
        {
            CoreModelEditStepLibrary.UpdateParcelUse(parcelIds, newUse);
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
        }

        public static void ReplaceRhinoModelPipeline(RhinoDoc doc) =>
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);

        public static void RefreshStudyAreaMetricsPipeline()
        {
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
        }

        public static void AddOrUpdateParcelDividerPipeline(RhinoDoc doc, 
            List<DividerData> dividerData)
        {
            CoreModelEditStepLibrary
                .TryRemoveDivders(dividerData.Select(x => x.ID).ToList());
            CoreModelEditStepLibrary.AddDivders(dividerData);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
        }
        public static void DeleteParcelDividers(RhinoDoc doc, List<Guid> dividerIds)
        {
            if (!CoreModelEditStepLibrary.TryRemoveDivders(dividerIds)) return;
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
        }

        internal static void AddOrUpdateBuldingPipeline(
            RhinoDoc doc,
            List<BuildingData> buildingData)
        {
            CoreModelEditStepLibrary.AddOrUpdateBulding(buildingData);
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
        }

        internal static void DeleteBuildingPipeline(
            RhinoDoc doc, 
            List<BuildingData> buildingData)
        {
            if (buildingData.Count == 0) return;
            CoreModelEditStepLibrary.TryDeleteBuildings(buildingData);
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
        }
        internal static void AddStudyAreaPipeline(RhinoDoc doc, List<StudyData> studyData)
        {
            CoreModelEditStepLibrary.AddOrUpdateStudyArea(studyData);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
        }

        internal static void DeleteStudyAreaPipeline(RhinoDoc doc, List<Guid> guids)
        {
            CoreModelEditStepLibrary.DeleteStudyAreas(guids);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshBuildingUse();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
        }

        internal static void UpdateStudyAreas(RhinoDoc doc, List<StudyData> studyData)
        {
            AddStudyAreaPipeline(doc, studyData);
        }

        internal static void ReloadAppPipeline(RhinoDoc doc)
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
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
        }

        internal static void RemoveRoadPipeline(List<TransitData> transitData, 
            RhinoDoc doc)
        {
            CoreModelEditStepLibrary.TryRemoveRoad(transitData);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcelsAndTransit();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
        }
    }
}
