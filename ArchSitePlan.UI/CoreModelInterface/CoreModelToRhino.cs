using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

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
        public static void OutputParcelsToRhino(List<Parcel> parcels, RhinoDoc doc)
        {
            // Get Attributes
            var parcelGeometryAttributes = GetParcelAttributesForAllUses(doc);
            var parcelLayerNames = Enum.GetValues<Landuse>()
                .Select(use => LayerHelpers.GetParcelRhinoLayerName(use))
                .ToList();

            var parcelLayerIndices = doc.Layers
                .Where(layer => parcelLayerNames.Contains(layer.Name))
                .Select(layer => layer.Index)
                .ToList();

            var parcelGuids = parcels.Select(p => p.OriginalObjectID).ToList();

            // Delete Existing Parcel Objects
            var guids = doc.Objects
                .Where(obj => parcelGuids.Contains(obj.Id))
                .Select(obj => obj.Id)
                .ToList();

            doc.Objects.Delete(guids, true);

            // Bake model to rhino
            foreach(var parcel in parcels)
            {
                var rhinoSrf = parcel.Boundary.ToRhino();
                parcel.OriginalObjectID = doc.Objects.AddBrep(rhinoSrf, parcelGeometryAttributes[parcel.LandUse]);
            }

            doc.Views.Redraw();
        }

        public static void OutputCoreModelToRhino(RhinoDoc doc)
        {
            LayerHelpers.ToggleLockLayer(doc, LayerConstants.MainLayer, false);
            OutputRoadsToRhino(doc);
            OutputParcelsToRhino(CoreModel.Parcels, doc);
            MoveStudyAreasToCorrectRhinoLayer(doc);
            LayerHelpers.ToggleLockLayer(doc, LayerConstants.MainLayer, true);
        }

        private static void MoveStudyAreasToCorrectRhinoLayer(RhinoDoc doc)
        {
            // Get Attributes
            LayerHelpers.EnsureLayerExists(doc, LayerConstants.StudyAreaLayer);
            int studyAreasLayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                RhinoDoc.ActiveDoc, 
                LayerConstants.StudyAreaLayer,
                LayerConstants.StudyAreaColour);

            ObjectAttributes studyAreasAttributes = new ObjectAttributes()
            {
                LayerIndex = studyAreasLayerIndex
            };

            // Delete Existing Objects
            var rhinoGuidsInLayer = doc.Objects
                .Where(obj => obj.Attributes.LayerIndex == studyAreasLayerIndex)
                .Select(obj => obj.Id)
                .ToList();

            var studiesAndMetrics = CoreModel.StudiesAndMetrics
                .Where(s => !rhinoGuidsInLayer.Contains(s.Study.Id))
                .ToList();

            foreach( var studyAndMetric in studiesAndMetrics )
            {
                var studyAreaObj = doc.Objects.FindId(studyAndMetric.Study.Id);
                studyAreaObj.Attributes.LayerIndex = studyAreasLayerIndex;
                studyAreaObj.CommitChanges();
            }

            doc.Views.Redraw();
        }

        private static void OutputRoadsToRhino(RhinoDoc doc)
        {
            // Get Attributes
            LayerHelpers.EnsureLayerExists(doc, LayerConstants.RoadLayer);

            ObjectAttributes roadGeometryAttributes = new ObjectAttributes()
            {
                LayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                    RhinoDoc.ActiveDoc, 
                    LayerConstants.RoadLayer,
                    LayerConstants.RoadColour)
            };

            // Delete Existing Objects
            var guids = doc.Objects
                .Where(obj => obj.Attributes.LayerIndex == roadGeometryAttributes.LayerIndex)
                .Select(obj => obj.Id)
                .ToList();

            doc.Objects.Delete(guids, true);

            // Bake model to rhino
            var rhinoSurfaces = CoreModel.ROWs
                .Select(row => row.ToRhino())
                .ToList();

            rhinoSurfaces
                .ToList()
                .ForEach(brep => doc.Objects.AddBrep(brep, roadGeometryAttributes));

            doc.Views.Redraw();
        }

        internal static void UpdateRhinoParcel(Guid id)
        {
            var parcel = CoreModel.Parcels.FirstOrDefault(p => p.OriginalObjectID == id);

            if(parcel != null)
                OutputParcelsToRhino(parcel.ObjectToList(), RhinoDoc.ActiveDoc);
        }

        internal static void BakeRhinoBuilding(Building building)
        {
            var extrusion = building.GetExtruded3D();

            var ex = Rhino.Geometry.Extrusion.Create(extrusion.Curve.ToRhino(),
                extrusion.Direction.Length(),
                extrusion.Capped);
            
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            // Create new building
            building.OutputMassId = doc.Objects.AddExtrusion(ex, RhinoObjAttributeConstants.BuildingGeometryAttributes);
            for(int i =0; i< building.FloorPlates.Count; i++)
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
            var guids = building.OutputMassId.ObjectToList();
            guids.AddRange(building.FloorPlates.Select(f => f.Id));

            // Delete rhino objects
            var result = guids.Select(id => doc.Objects.FindId(id))
                .Where(obj => obj != null)
                .Select(obj => doc.Objects.Delete(obj, true, true))
                .ToList();
        }
    }
}
