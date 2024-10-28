using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Actions;
using BH.Engine.Reflection;
using BH.oM.Geometry;
using Rhino;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.UI.CoreModelInterface
{
    public static class ModelBuilderPipelines
    {

        public static void AddOrUpdateRoadPipeline(RhinoDoc doc,
            List<(Polyline? centerLine, Guid id)> centerLineAndIds,
            double width)
        {
            CoreModelEditStepLibrary.TryRemoveRoad(centerLineAndIds.Select(x => x.id));
            CoreModelEditStepLibrary.AddOrUpdateRoad(centerLineAndIds, width);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcels();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
        }

        public static void ChangeParcelUsePipeline(
            RhinoDoc doc,
            List<Guid> parcelIds,
            Landuse newUse)
        {
            CoreModelEditStepLibrary.UpdateParcelUse(parcelIds, newUse);
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
        }

        public static void ReplaceRhinoModelPipeline(RhinoDoc doc) =>
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);

        public static void RefreshStudyAreaMetricsPipeline()
        {
            CoreModelEditStepLibrary.DivideParcels();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
        }

        public static void AddOrUpdateParcelDividerPipeline(RhinoDoc doc, 
            List<(Polyline? Crv, Guid ID)> plineObjs)
        {
            CoreModelEditStepLibrary
                .TryRemoveDivders(plineObjs.Select(x => x.ID).ToList());
            CoreModelEditStepLibrary.AddDivders(plineObjs);
            CoreModelEditStepLibrary.DivideParcels();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
        }
        public static void DeleteParcelDividers(RhinoDoc doc, List<Guid> dividerIds)
        {
            if (!CoreModelEditStepLibrary.TryRemoveDivders(dividerIds)) return;
            CoreModelEditStepLibrary.DivideParcels();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc);
        }

        internal static void AddOrUpdateBuldingPipeline(
            RhinoDoc doc,
            List<(Mesh? Envelope, Guid ID, List<PlanarSurface> FloorPlates)> buildingData,
            double ffHt,
            int floorCount)
        {
            //RhinoModelEditStepLibrary.DeleteRhinoBuilding(doc, 
            //    buildingData.Select(d => d.ID).ToList());

            CoreModelEditStepLibrary.AddOrUpdateBulding(buildingData, ffHt, floorCount);
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
        }

        internal static void DeleteBuildingPipeline(RhinoDoc doc, List<Guid> buildingEnvelopeGuids)
        {
            if (buildingEnvelopeGuids.Count == 0) return;
            RhinoModelEditStepLibrary.DeleteRhinoBuilding(doc, buildingEnvelopeGuids);
            CoreModelEditStepLibrary.TryDeleteBuildings(buildingEnvelopeGuids);
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
        }
        internal static void AddStudyAreaPipeline(RhinoDoc doc, List<(Polyline, Guid, string)> studyData)
        {
            CoreModelEditStepLibrary.AddOrUpdateStudyArea(studyData);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcels();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
        }

        internal static void DeleteStudyAreaPipeline(RhinoDoc doc, List<Guid> guids)
        {
            CoreModelEditStepLibrary.DeleteStudyAreas(guids);
            CoreModelEditStepLibrary.RecalculateParcelsAndRoadRoWs();
            CoreModelEditStepLibrary.DivideParcels();
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
        }

        internal static void UpdateStudyAreas(RhinoDoc doc, List<(Polyline Crv, Guid Id)> plineObjs)
        {
            var studyData = CoreModelEditStepLibrary.BuildStudyDataForValidStudies(plineObjs);

            if (studyData.Count == 0) return;

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
            CoreModelEditStepLibrary.DivideParcels();

            //Output
            RhinoModelEditStepLibrary.ReplaceRhinoModel(doc, true);
            CoreModelEditStepLibrary.RefreshStudyAreaMetrics();
        }
    }
}
