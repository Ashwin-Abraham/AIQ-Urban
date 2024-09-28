using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Data;
using BH.oM.Geometry;
using Rhino;
using Rhino.DocObjects;
using static ArchSitePlan.Core.Constants.Enums;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI.Actions
{
    public static class CoreModelToRhino
    {
        private static ObjectAttributes GetParcelAttributesByUse(RhinoDoc doc, Landuse use)
        {
            string layerName = LayerHelpers.GetParcelRhinoLayerName(use);
            int layerIndex = LayerHelpers.CreateOrGetLayerIndex(
                doc,
                layerName,
                Defaults.LandUseColours[use]);

            LayerHelpers.SetLayerColour(doc, layerIndex, Defaults.LandUseColours[use]);

            ObjectAttributes parcelGeometryAttributes = new ObjectAttributes()
            {
                LayerIndex = layerIndex
            };

            return parcelGeometryAttributes;
        }
        private static Dictionary<Landuse, ObjectAttributes> GetParcelAttributesForAllUses(RhinoDoc doc)
        {
            var uses = Enum.GetValues(typeof(Landuse)).Cast<Landuse>().ToList();
            var dictionary = new Dictionary<Landuse, ObjectAttributes>();

            foreach ( var use in uses)
            {
                var attributes = GetParcelAttributesByUse(doc, use);
                dictionary.Add(use, attributes);
            }

            return dictionary;
        }
        public static void OutputParcelsToRhino(RhinoDoc doc)
        {
            // Get Attributes
            var parcelGeometryAttributes = GetParcelAttributesForAllUses(doc);
            var parcelLayerNames = LayerHelpers.GetParcelRhinoLayerNames();

            var layerNames = doc.Layers.Select(l => l.Name).ToList();

            var parcelObjs = doc.Layers
                .Where(layer => parcelLayerNames.Contains(layer.FullPath))
                .SelectMany(lay => doc.Objects.FindByLayer(lay))
                .ToList();

            parcelObjs.ForEach(obj => doc.Objects.Delete(obj, true, true));
            
            // Bake model to rhino
            foreach(var parcel in CoreModel.Parcels)
            {
                var rhinoSrf = parcel.Boundary.ToRhino();
                parcel.OriginalObjectID = doc.Objects.AddBrep(rhinoSrf, parcelGeometryAttributes[parcel.LandUse]);
            }

            doc.Views.Redraw();
        }

        public static void OutputCoreModelToRhino(RhinoDoc doc)
        {
            OutputRoadOutlinesToRhino(doc);
            OutputParcelsToRhino(doc);
            OutputStudyAreasToRhino(doc);
        }

        private static void OutputStudyAreasToRhino(RhinoDoc doc)
        {
            var studies = CoreModel.Studies;

            foreach(var study in studies)
            {
                string layerName = LayerConstants.StudyAreaLayer + LayerConstants._Sep_ + study.Name;
                LayerHelpers.EnsureLayerExists(doc, layerName);

                // Get Attributes
                int studyAreasLayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                    RhinoDoc.ActiveDoc,
                    layerName,
                    LayerConstants.StudyAreaColour);

                ObjectAttributes studyAreasAttributes = new ObjectAttributes()
                {
                    LayerIndex = studyAreasLayerIndex,
                };

                // Delete existing boundary objects
                var studyObjs = doc.Objects.Where(obj => obj.Attributes.LayerIndex == studyAreasLayerIndex).ToList();
                studyObjs.Add(doc.Objects.FindId(study.Id));
                studyObjs.RemoveAll(o => o == null);
                studyObjs.ForEach(o => doc.Objects.Delete(o, true, true));

                // Bake boundary
                if (study.Boundary == null) continue;
                var boundary = study.Boundary.ToRhino();
                doc.Objects.Add(boundary, studyAreasAttributes);
            }
            
            doc.Views.Redraw();
        }

        private static void OutputRoadOutlinesToRhino(RhinoDoc doc)
        {
            // Get Attributes
            LayerHelpers.EnsureLayerExists(doc, LayerConstants.RoadOutlineLayer);

            ObjectAttributes roadOutlineObjAttributes = new ObjectAttributes()
            {
                LayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                    RhinoDoc.ActiveDoc, 
                    LayerConstants.RoadOutlineLayer,
                    LayerConstants.RoadColour)
            };

            // Delete Existing Objects
            var guids = doc.Objects
                .Where(obj => obj.Attributes.LayerIndex == roadOutlineObjAttributes.LayerIndex)
                .Select(obj => obj.Id)
                .ToList();

            doc.Objects.Delete(guids, true);

            // Bake model to rhino
            var rhinoSurfaces = CoreModel.ROWs
                .Select(row => row.ToRhino())
                .ToList();

            rhinoSurfaces
                .ToList()
                .ForEach(brep => doc.Objects.AddBrep(brep, roadOutlineObjAttributes));

            // ROAD CENTERLINES
            // Find Layer info and ensure existence
            var roadClLayerNames = CoreModel.Roads
                .Select(r => r.Width.Round(CoreGeneralConstants.TransitRouteWidthPrecision))
                .Select(width => LayerHelpers.GenerateRoadCLLayer(width))
                .ToList();

            roadClLayerNames.ForEach(name => LayerHelpers.EnsureLayerExists(doc, name));

            var roadCLLayers = doc.Layers
                .Where(lay => roadClLayerNames.Contains(lay.FullPath))
                .ToList();

            // Find road CL rhino objects
            var coreModelClGuids = CoreModel.Roads.Select(r => r.CenterLineId).ToList();

            var rhinoCLs = doc.Objects
                .Where(obj => coreModelClGuids.Contains(obj.Id))
                .ToList();

            var roadAndLayer = rhinoCLs.Select(obj =>
                (rhinoObj: obj,
                layer: CoreModel.Roads.FirstOrDefault(r => r.CenterLineId == obj.Id)))
                .ToList();

            // Ensure each CL is on required layer
            foreach (var obj in rhinoCLs)
            {
                double width = CoreModel.Roads.FirstOrDefault(r => r.CenterLineId == obj.Id)
                    .Width.Round(CoreGeneralConstants.TransitRouteWidthPrecision);
                string layerName = LayerHelpers.GenerateRoadCLLayer(width);
                
                int? layerIndex = roadCLLayers
                    .FirstOrDefault(lay => lay.FullPath == layerName)?
                    .Index;

                if(layerIndex.HasValue)
                    LayerHelpers.ChangeLayer(obj, layerIndex.Value);
            }

            doc.Views.Redraw();
        }

        internal static void BakeRhinoBuilding(Building building)
        {
            var rhinoMesh = building.Envelope.ToRhino();
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            
            // Create new building
            building.EnvelopeId = doc.Objects.AddMesh(rhinoMesh, RhinoObjAttributeConstants.BuildingGeometryAttributes);

            for (int i = 0; i < building.FloorPlates.Count; i++)
            {
                building.FloorPlates[i] =
                    (Profile: building.FloorPlates[i].Profile,
                    Id: BakeToRhino(
                        building.FloorPlates[i].Profile,
                        doc,
                        RhinoObjAttributeConstants.BuildingFloorPlateAttributes));
            }
        }

        private static Guid BakeToRhino(PlanarSurface floorProfile, RhinoDoc doc, ObjectAttributes attrib)
        {
            var curve = floorProfile.ExternalBoundary.ToRhino();
            Guid id = doc.Objects.AddCurve(curve, attrib);
            return id;
        }

        internal static void DeleteRhinoBuilding(Building building)
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            // Find guids
            var guids = building.EnvelopeId.ObjectToList();
            guids.AddRange(building.FloorPlates.Select(f => f.Id));

            // Delete rhino objects
            var result = guids.Select(id => doc.Objects.FindId(id))
                .Where(obj => obj != null)
                .Select(obj => doc.Objects.Delete(obj, true, true))
                .ToList();
        }

        internal static void RefreshStudyAreaMetrics(bool recalculateModelSplit = true)
        {
            CoreModel.RefreshStudyAreaMetrics(recalculateModelSplit);
        }

        internal static void LockModelLayersExcept(RhinoDoc doc, List<string> layersToUnlock, bool unlockChildren=true)
        {
            ToggleLockModelLayers(doc, true);

            // Unlock layer and children
            List<Layer> unlockedLayers = new List<Layer>();
            foreach(var layer in layersToUnlock)
            {
                int layerIndex = doc.Layers.FindByFullPath(layer, -1);
                if (layerIndex == -1) continue;

                Layer parent = doc.Layers[layerIndex];
                var children = parent.GetChildren(true);

                LayerHelpers.ToggleLockLayer(doc, layer, false);
                unlockedLayers.Add(parent);
                parent.IsLocked = false;

                if (children != null)
                {
                    foreach (var child in children)
                        child.IsLocked = false;

                    unlockedLayers.AddRange(children);
                }
            }
        }

        internal static void ToggleLockModelLayers(RhinoDoc doc, bool lockLayers)
        {
            var mainLayer = doc.Layers.FindByFullPath(LayerConstants.MainLayer, -1);
            if (mainLayer == -1) return;

            var appLayers = doc.Layers[mainLayer].GetChildren(true);

            // Unlock app layers
            doc.Layers[mainLayer].IsLocked = false;
            foreach (var layer in appLayers)
                layer.IsLocked = lockLayers; 
        }

        internal static void ToggleLayerStateBasedOnAppMode(RhinoDoc doc, AppMode appMode)
        {
            List<string> layersToUnlock = new();
            List<string> additionalLayersToLock = new();

            switch (appMode)
            {
                case AppMode.Road:  layersToUnlock.Add(LayerConstants.RoadLayer); break;
                case AppMode.LandUse:  layersToUnlock.Add(LayerConstants.ParcelsLayer); break;
                case AppMode.Building:
                    {
                        layersToUnlock.Add(LayerConstants.BuildingLayer);
                        additionalLayersToLock.Add(LayerConstants.BuildingFloorOutlineLayer);
                    }
                    break;
                case AppMode.StudyArea:  layersToUnlock.Add(LayerConstants.StudyAreaLayer); break;
                case AppMode.None:  layersToUnlock.Add(LayerConstants.MainLayer); break;
            }

            // Manipulate layer lock states
            LockModelLayersExcept(doc, layersToUnlock);
            additionalLayersToLock.ForEach(layer => LayerHelpers.ToggleLockLayer(doc, layer, true));

            // Deselects selected objects
            doc.Views.Redraw();
        }

        internal static void TryDeleteBakedBuilding(RhinoDoc doc, params RhinoObject[] objects)
        {
            foreach (var obj in objects)
                doc.Objects.Delete(obj, true, true);
        }

        internal static void TryDeleteBakedBuilding(params Guid[] ids)
        {
            ids = ids.Where(id => id!=Guid.Empty).ToArray();
            List<Guid> buildingIdsToDelete = new();

            var buildings = CoreModel.Buildings.FindAll(b => ids.Contains(b.FootprintId) || ids.Contains(b.EnvelopeId));

            foreach(var building in buildings)
            {
                buildingIdsToDelete.Add(building.FootprintId);
                buildingIdsToDelete.Add(building.EnvelopeId);
                buildingIdsToDelete.AddRange(building.FloorPlates.Select(f => f.Id));
            }

            DocObjectHelpers.DeleteRhinoObjectsIfPresent(RhinoDoc.ActiveDoc, buildingIdsToDelete);
        }
    }
}
