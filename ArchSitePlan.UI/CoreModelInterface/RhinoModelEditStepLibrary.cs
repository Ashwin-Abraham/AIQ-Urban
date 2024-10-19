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
using Rhino.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.UI.CoreModelInterface
{
    internal class RhinoModelEditStepLibrary
    {
        public static void ReplaceRhinoModel(RhinoDoc doc, bool includingBuildings = false)
        {
            OutputRoadOutlinesToRhino(doc);
            OutputParcelsToRhino(doc);
            OutputStudyAreasToRhino(doc);

            if (includingBuildings)
                BakeRhinoBuilding(doc);

            doc.Views.Redraw();
        }
        public static void DeleteRhinoBuilding(RhinoDoc doc, List<Guid> envelopeIds)
        {
            // Find all building guids
            var guids = new List<Guid>(envelopeIds);
            var buildings = CoreModel.Buildings.Where(b => envelopeIds.Contains(b.EnvelopeId));
            guids.AddRange(buildings.SelectMany(b => b.FloorPlates.Select(f => f.Id)));

            // Delete rhino objects
            var result = guids.Select(id => doc.Objects.FindId(id))
                .Where(obj => obj != null)
                .Select(obj => doc.Objects.Delete(obj, true, true))
                .ToList();
        }
        internal static void BakeRhinoBuilding(RhinoDoc doc)
        {
            foreach(var building in CoreModel.Buildings)
            {
                var rhinoMesh = building.Envelope.ToRhino();

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
           
        }
        private static Guid BakeToRhino(PlanarSurface floorProfile, 
            RhinoDoc doc, 
            ObjectAttributes attrib)
        {
            var curve = floorProfile.ExternalBoundary.ToRhino();
            Guid id = doc.Objects.AddCurve(curve, attrib);
            return id;
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
            var rhinoObjs = doc.Objects
                .Where(obj => obj.Attributes.LayerIndex == roadOutlineObjAttributes.LayerIndex)
                .ToList();

            rhinoObjs.ForEach(obj => doc.Objects.Delete(obj, true, true));

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

            //var roadAndLayer = rhinoCLs.Select(obj =>
            //    (rhinoObj: obj,
            //    layer: CoreModel.Roads.FirstOrDefault(r => r.CenterLineId == obj.Id)))
            //    .ToList();

            // Ensure each CL is on required layer
            foreach (var obj in rhinoCLs)
            {
                double width = CoreModel.Roads.FirstOrDefault(r => r.CenterLineId == obj.Id)
                    .Width.Round(CoreGeneralConstants.TransitRouteWidthPrecision);
                string layerName = LayerHelpers.GenerateRoadCLLayer(width);

                int? layerIndex = roadCLLayers
                    .FirstOrDefault(lay => lay.FullPath == layerName)?
                    .Index;

                if (layerIndex.HasValue)
                    LayerHelpers.ChangeLayer(obj, layerIndex.Value);
            }
        }

        private static void OutputStudyAreasToRhino(RhinoDoc doc)
        {
            var studies = CoreModel.Studies;
            List<(Curve, ObjectAttributes)> studyBoundaries = new();
            foreach (var study in studies)
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
                studyBoundaries.Add((boundary, studyAreasAttributes));
            }

            studyBoundaries.ForEach(s => doc.Objects.Add(s.Item1, s.Item2));
        }

        private static void OutputParcelsToRhino(RhinoDoc doc)
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
            foreach (var parcel in CoreModel.Parcels)
            {
                var rhinoSrf = parcel.Boundary.ToRhino();
                if (rhinoSrf != null)
                    parcel.OriginalObjectID
                        = doc.Objects.AddBrep(rhinoSrf, parcelGeometryAttributes[parcel.LandUse]);
            }
        }
        private static Dictionary<Landuse, ObjectAttributes> GetParcelAttributesForAllUses(RhinoDoc doc)
        {
            var uses = Enum.GetValues(typeof(Landuse)).Cast<Landuse>().ToList();
            var dictionary = new Dictionary<Landuse, ObjectAttributes>();

            foreach (var use in uses)
            {
                var attributes = GetParcelAttributesByUse(doc, use);
                dictionary.Add(use, attributes);
            }

            return dictionary;
        }

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


    }
}
