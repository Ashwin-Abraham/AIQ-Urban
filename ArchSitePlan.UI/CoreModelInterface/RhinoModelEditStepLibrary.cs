using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.UI.Actions;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Data;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.CoreModelInterface
{
    internal class RhinoModelEditStepLibrary
    {
        public static List<DividerData> ExtractDividerData(RhinoDoc doc)
        {
            List<DividerData> dividerData = new();

            // STUDY AREAS
            // Read boundary objects
            int dividerLayerIndex = doc.Layers.FindByFullPath(
                LayerConstants.ParcelDividerLayer, 
                -1);

            if (dividerLayerIndex == -1) return dividerData;

            var dividerObjs = doc.Objects
                .Where(o => o.Attributes.LayerIndex == dividerLayerIndex)
                .ToList();

            foreach(var obj in dividerObjs)
            {
                var geo = obj.Geometry as Curve;
                if (geo is null) continue;

                if (!geo.TryGetPolyline(out var pline)) continue;

                var divData = new DividerData(
                    pline.ToBHoMPolyline(),
                    obj.Id);

                dividerData.Add(divData);
            }

            return dividerData;
        }
        public static List<BuildingData> ExtractBuildingCreationDataFromRhino(RhinoDoc doc)
        {
            List<BuildingData> buildingData = new();
            CoreModel.Buildings = new();

            // Read mesh envelope objects
            int envelopeLayer = doc.Layers.FindByFullPath(LayerConstants.BuildingMassLayer, true);
            int floorLayer = doc.Layers.FindByFullPath(LayerConstants.BuildingFloorOutlineLayer, true);

            if (envelopeLayer == -1 || floorLayer == -1) return buildingData;

            var meshEnvelopeObjs = doc.Objects
                .Where(o => o.Attributes.LayerIndex == envelopeLayer && o.ObjectType == ObjectType.Mesh)
                .ToList();

            var floorOutlines = doc.Objects
                .Where(o => o.Attributes.LayerIndex == floorLayer && o.ObjectType == ObjectType.Curve)
                .ToList();

            // Group mesh and floor outlines together
            Dictionary<RhinoObject, List<RhinoObject>> buildingEnvelopeAndFloors = new();
            foreach (var floor in floorOutlines)
            {
                var testPoint = (floor.Geometry as Curve).PointAtStart;
                var nearestMesh = meshEnvelopeObjs.MinBy(m => (m.Geometry as Rhino.Geometry.Mesh).ClosestPoint(testPoint).DistanceTo(testPoint));

                if (buildingEnvelopeAndFloors.ContainsKey(nearestMesh))
                    buildingEnvelopeAndFloors[nearestMesh].Append(floor);
                else
                    buildingEnvelopeAndFloors.Add(nearestMesh, floor.ObjectToList());
            }

            // Add a building for each dictionary entry
            foreach (var entry in buildingEnvelopeAndFloors)
            {
                var envelope = (entry.Key.Geometry as Rhino.Geometry.Mesh).ToBHoM();
                var floors = entry.Value
                    .Select(f => (f.Geometry as Curve).ToBHoM())
                    .Select(c => new PlanarSurface(c, new List<ICurve>()))
                    .ToList();

                double ffHt = floors.Count > 1 ?
                    floors[0].IBounds().Min.Z - floors[1].IBounds().Min.Z
                    : envelope.Bounds().Height();

                ffHt = Math.Abs(ffHt);

                var data = new BuildingData(envelope, floors, entry.Key.Id, Guid.Empty, ffHt);
                buildingData.Add(data);
            }

            return buildingData;
        }
        public static List<StudyData> ExtractStudyAreasFromRhino(RhinoDoc doc)
        {
            CoreModel.ResetStudyAreas();
            List<StudyData> studyData = new();

            // STUDY AREAS
            // Read boundary objects
            int studyLayer = doc.Layers.FindByFullPath(LayerConstants.StudyAreaLayer, true);

            if (studyLayer == -1) return studyData;

            var studyLayerIndices = doc.Layers[studyLayer].GetChildren().Select(lay => lay.Index).ToList();

            foreach (var index in studyLayerIndices)
            {
                var studyObj = doc.Objects
                    .Where(o => o.Attributes.LayerIndex == index)
                    .FirstOrDefault(s =>
                    {
                        var crv = s.Geometry as Curve;
                        if (crv == null) return false;
                        return crv.TryGetPolyline(out var _);
                    });

                if (studyObj == null) continue;
                (studyObj.Geometry as Curve).TryGetPolyline(out var pline);

                studyData.Add(new StudyData
                    (pline.ToBHoMPolyline(),
                    studyObj.Id,
                    doc.Layers[index].Name));
            }

            return studyData;
        }
        public static List<TransitData> ExtractTransitFromRhino(RhinoDoc doc)
        {
            List<TransitData> transitData = new();

            // TRANSIT
            // Read Road CL objects
            int roadCLMainLayerIndex = doc.Layers.FindByFullPath(LayerConstants.RoadCLLayer, -1);

            if (roadCLMainLayerIndex == -1) return transitData;

            var childLayers = doc
                .Layers[roadCLMainLayerIndex]
                .GetChildren();

            if (childLayers is null || childLayers.Length == 0) return transitData;

            var roadCLLayerIndices = childLayers
                .Select(lay => lay.Index).ToList();

            foreach (var index in roadCLLayerIndices)
            {
                var roadCLs = doc.Objects.Where(o => o.Attributes.LayerIndex == index).ToList();

                if (!Double.TryParse(doc.Layers[index].Name, out double roadWidth))
                    continue;

                roadWidth = roadWidth.Round(CoreGeneralConstants.TransitRouteWidthPrecision);

                foreach (var obj in roadCLs)
                {
                    var curve = obj.Geometry as Curve;
                    if (curve == null) continue;

                    var bhomCrv = curve.ToBHoM2DegreeCrv();
                    if (bhomCrv == null) continue;

                    transitData.Add(new TransitData
                        (bhomCrv!, obj.Id, roadWidth));
                }

            }
            return transitData;
        }
        public static void ReplaceRhinoModel(RhinoDoc doc, bool includingBuildings = false)
        {
            bool initialState = RhinoEventHandler.EventLock;
            RhinoEventHandler.EventLock = true;

            OutputRoadsToRhino(doc);
            OutputParcelsToRhino(doc);
            OutputStudyAreasToRhino(doc);

            BakeRhinoBuilding(doc);

            RemoveEmptyAppLayers(doc);
            doc.Views.Redraw();
            RhinoEventHandler.EventLock = initialState;
            RhinoEventHandler.ResetEventQueueAndObjects();
        }

        private static void RemoveEmptyAppLayers(RhinoDoc doc)
        {
            LayerHelpers.CreateOrGetLayerIndex(doc, LayerConstants.MainModellingLayer);

            var mainLayerIndex = doc.Layers.FindByFullPath(LayerConstants.MainModellingLayer, -1);
            
            if (mainLayerIndex == -1) return;
            LayerHelpers.DeleteLayerAndChildrenIfEmpty(doc, doc.Layers[mainLayerIndex]);
        }

        public static void DeleteAllRhinoBuilding(RhinoDoc doc)
        {
            var buildingLayerIndex = doc.Layers.FindByFullPath(LayerConstants.BuildingLayer, -1);
            if (buildingLayerIndex == -1) return;

            List<Layer> allBldgLayers = doc.Layers[buildingLayerIndex].GetChildren(true)?.ToList()?? new();

            allBldgLayers.Add(doc.Layers[buildingLayerIndex]);

            var buildingLayerIndices = allBldgLayers
                .Where(lay => lay is not null)
                .Select(x => x.Index).ToList();
            
            // Delete rhino objects
            var result = doc.Objects.Where(o => buildingLayerIndices.Contains(o.Attributes.LayerIndex))
                .Select(obj => doc.Objects.Delete(obj, true, true))
                .ToList();
        }
        internal static void BakeRhinoBuilding(RhinoDoc doc)
        {
            DeleteAllRhinoBuilding(doc);

            int outlineLayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                RhinoDoc.ActiveDoc,
                LayerConstants.BuildingFloorOutlineLayer,
                LayerConstants.BuildingFloorOutlineColour);

            int massLayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                RhinoDoc.ActiveDoc,
                LayerConstants.BuildingMassLayer,
                LayerConstants.BuildingMassColour);

            var outlineAttrib = new ObjectAttributes()
            {
                LayerIndex = outlineLayerIndex
            };

            var massAttrib = new ObjectAttributes()
            {
                LayerIndex = massLayerIndex,
                ColorSource = ObjectColorSource.ColorFromObject
            };

            foreach (var building in CoreModel.Buildings)
            {
                var rhinoMesh = building.Envelope.ToRhino();
                massAttrib.ObjectColor = building.Use.GetBuildingColor();

                // Create new building
                building.EnvelopeId = doc.Objects.AddMesh(rhinoMesh, massAttrib);
                for (int i = 0; i < building.FloorPlates.Count; i++)
                {
                    var curve = building.FloorPlates[i].Profile.ExternalBoundary.ToRhino();
                    var newId = doc.Objects.AddCurve(curve, outlineAttrib);
                    building.FloorPlates[i] = (building.FloorPlates[i].Profile, newId);
                }
            }
           
        }

        private static void OutputRoadsToRhino(RhinoDoc doc)
        {
            // Delete Existing Objects
            LayerHelpers.DeleteLayerAndChildren(doc, LayerConstants.RoadLayer);

            // Get Attributes
            LayerHelpers.CreateOrGetLayerIndex(doc, LayerConstants.RoadOutlineLayer);

            ObjectAttributes roadOutlineObjAttributes = new ObjectAttributes()
            {
                LayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                    RhinoDoc.ActiveDoc,
                    LayerConstants.RoadOutlineLayer,
                    LayerConstants.RoadColour)
            };

            // Bake model to rhino
            var rhinoSurfaces = CoreModel.ROWs
                .Select(row => row.ToRhino())
                .ToList();

            rhinoSurfaces
                .ToList()
                .ForEach(brep => doc.Objects.AddBrep(brep, roadOutlineObjAttributes));

            // ROAD CENTERLINES
            // Output road cls
            foreach (var road in CoreModel.Roads)
            {
                double width = road.Width
                    .Round(CoreGeneralConstants.TransitRouteWidthPrecision);

                string layerName = LayerHelpers.GenerateRoadCLLayer(width);
                int layerIndex =  LayerHelpers.CreateOrGetLayerIndex(doc, layerName);

                ObjectAttributes attrib = new ObjectAttributes()
                {
                    LayerIndex = layerIndex
                };
                road.CenterLineId = doc.Objects.AddCurve(road.Centerline.ToRhino(), attrib);
            }
        }

        private static void OutputStudyAreasToRhino(RhinoDoc doc)
        {
            var studies = CoreModel.Studies;
            List<(Curve, ObjectAttributes)> studyBoundaries = new();
            foreach (var study in studies)
            {
                string layerName = LayerConstants.StudyAreaLayer + LayerConstants._Sep_ + study.Name;
                LayerHelpers.CreateOrGetLayerIndex(doc, layerName);

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

                study.Id = doc.Objects.Add(boundary, studyAreasAttributes);
            }
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
                    parcel.Id
                        = doc.Objects.AddBrep(rhinoSrf, parcelGeometryAttributes[parcel.LandUse]);
            }
        }
        private static Dictionary<Landuse, ObjectAttributes> GetParcelAttributesForAllUses(RhinoDoc doc)
        {
            var uses = AppLanduses.Uses;
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
                use.Color);

            LayerHelpers.SetLayerColour(doc, layerIndex, use.Color);

            ObjectAttributes parcelGeometryAttributes = new ObjectAttributes()
            {
                LayerIndex = layerIndex
            };

            return parcelGeometryAttributes;
        }

        internal static List<(BH.oM.Geometry.Point point, Landuse use)> ExtractRhinoLanduseData(RhinoDoc doc)
        {
            var parcelLayIndices = LayerHelpers
                .GetParcelRhinoLayerNames()
                .Select(name => doc.Layers.FindByFullPath(name, -1))
                .ToList();

            var rhinoParcelObjs = doc.Objects
                .Where(obj => parcelLayIndices.Contains(obj.Attributes.LayerIndex)
                    && obj.ObjectType == ObjectType.Brep)
                .ToList();

            var outputs = new List<(BH.oM.Geometry.Point point, Landuse use)>();
            foreach (var obj in rhinoParcelObjs)
            {
                var center = AreaMassProperties.Compute(obj.Geometry as Brep).Centroid;
                string layerName = doc.Layers[obj.Attributes.LayerIndex].Name;

                if (AppLanduses.TryGetUse(layerName, out var landuse))
                    landuse = AppLanduses.Undefined;

                outputs.Add((center.ToBHoM(), landuse));
            }

            return outputs;
        }
    }
}
