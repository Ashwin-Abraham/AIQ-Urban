using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.ModelEditActions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Data;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using static ArchSitePlan.Core.Constants.Enums;
using Mesh = Rhino.Geometry.Mesh;
using Polyline = Rhino.Geometry.Polyline;
namespace ArchSitePlan.UI.Actions
{
    public static class RhinoToCoreModel
    {
        public static bool TryExtractBHoMPolyline(
            RhinoObject obj,
            out BH.oM.Geometry.Polyline? bhomPline)
        {
            Curve curve = obj.Geometry as Curve;
            bhomPline = null;

            if (curve.Degree > 2)
                throw new Exception("Curve degree is too high, please select line, polyline or arc only");

            if (curve.TryGetPolyline(out Polyline polyline))
            {
                bhomPline = polyline.ToBHoM();
                return bhomPline != null;
            }

            return false;
        }

        internal static bool TryFindRoad(Guid id, out Road road)
            => CoreModel.TryFindRoad(id,out road);

        internal static List<Parcel> GetParcel(List<Guid> ids)
        {
            if (ids == null) return new List<Parcel>();

            List<Parcel> list = CoreModel.Parcels?
                .Where(parcel => ids.Contains(parcel.OriginalObjectID))
                .ToList();
            
            return list?? new List<Parcel>();
        }

        internal static void RecalculateBuildingHostParcelIDs()
        {
            CoreModel.RecalculateBuildingHostParcelIDs();
        }

        internal static List<Building> FindBuildingsInParcels(List<Parcel> parcels)
        {
            return CoreModel.FindBuildingsInParcels(parcels);
        }

        internal static void ReloadProjectFromRhino(RhinoDoc doc)
        {
            // RESET MODEL
            // Clear Core Model
            CoreModel.ClearModel();

            // TRANSIT
            ReloadTransitFromRhino(doc);

            // STUDY AREAS
            // Read study area objects
            ReloadStudyAreasFromRhino(doc);
            ModelBuilderPipelines.RefreshStudyAreaMetricsPipeline();

            // PARCELS
            // Read parcel Objects and store center points
            var landuseData = ExtractRhinoLanduseData(doc);
            CoreModel.UpdateParcelUses(landuseData);

            // BUILDINGS
            // Read building objects
            ReloadBuildingsFromRhino(doc);
        }

        private static void ReloadBuildingsFromRhino(RhinoDoc doc)
        {
            CoreModel.Buildings = new();

            // Read mesh envelope objects
            int envelopeLayer = doc.Layers.FindByFullPath(LayerConstants.BuildingMassLayer, true);
            int floorLayer = doc.Layers.FindByFullPath(LayerConstants.BuildingFloorOutlineLayer, true);

            if (envelopeLayer == -1 || floorLayer == -1) return;

            var meshEnvelopeObjs = doc.Objects
                .Where(o => o.Attributes.LayerIndex == envelopeLayer && o.ObjectType == ObjectType.Mesh)
                .ToList();

            var floorOutlines = doc.Objects
                .Where(o => o.Attributes.LayerIndex == floorLayer && o.ObjectType == ObjectType.Curve)
                .ToList();

            // Group mesh and floor outlines together
            Dictionary<RhinoObject, List<RhinoObject>> buildingEnvelopeAndFloors = new();
            foreach(var floor in floorOutlines)
            {
                var testPoint = (floor.Geometry as Curve).PointAtStart;
                var nearestMesh = meshEnvelopeObjs.MinBy(m => (m.Geometry as Mesh).ClosestPoint(testPoint).DistanceTo(testPoint));

                if (buildingEnvelopeAndFloors.ContainsKey(nearestMesh))
                    buildingEnvelopeAndFloors[nearestMesh].Append(floor);
                else
                    buildingEnvelopeAndFloors.Add(nearestMesh, floor.ObjectToList());
            }

            // Add a building for each dictionary entry
            foreach(var entry in buildingEnvelopeAndFloors)
            {
                var envelope = (entry.Key.Geometry as Mesh).ToBHoM();
                var floors = entry.Value
                    .Select(f => (f.Geometry as Curve).ToBHoM())
                    .Select(c => new PlanarSurface(c, new List<ICurve>()))
                    .ToList();

                double ffHt = floors.Count > 1?
                    floors[0].IBounds().Min.Z - floors[1].IBounds().Min.Z
                    :envelope.Bounds().Height();

                ffHt = Math.Abs(ffHt);

                CoreModel.TryAddBuilding(envelope, floors, entry.Key.Id, Guid.Empty, ffHt);
            }
        }

        private static void ReloadStudyAreasFromRhino(RhinoDoc doc)
        {
            CoreModel.ResetStudyAreas();

            // STUDY AREAS
            // Read boundary objects
            int studyLayer = doc.Layers.FindByFullPath(LayerConstants.StudyAreaLayer, true);

            if (studyLayer == -1) return;

            var studyLayerIndices = doc.Layers[studyLayer].GetChildren().Select(lay => lay.Index).ToList();

            List<(BH.oM.Geometry.Polyline,
                Guid,
                string)> studyData = new();

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

                studyData.Add((pline.ToBHoM(),
                    studyObj.Id,
                    doc.Layers[index].Name));
            }

            ModelBuilderPipelines.AddStudyAreaPipeline(doc, studyData);
        }

        private static List<(BH.oM.Geometry.Point point, Landuse use)> ExtractRhinoLanduseData(RhinoDoc doc)
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
            foreach(var obj in rhinoParcelObjs)
            {
                var center = AreaMassProperties.Compute(obj.Geometry as Brep).Centroid;
                string layerName = doc.Layers[obj.Attributes.LayerIndex].Name;

                outputs.Add((center.ToBHoM(), ParseLanduse(layerName)));
            }

            return outputs;
        }

        private static Landuse ParseLanduse(string use)
        {
            if (Enum.TryParse<Landuse>(use, out var landuse))
                return landuse;
            else
                return Landuse.Undefined;
        }

        private static void ReloadTransitFromRhino(RhinoDoc doc)
        {
            // TRANSIT
            // Read Road CL objects
            int roadCLMainLayerIndex = doc.Layers.FindByFullPath(LayerConstants.RoadCLLayer, true);

            if (roadCLMainLayerIndex == -1) return;

            var roadCLLayerIndices = doc.Layers[roadCLMainLayerIndex].GetChildren().Select(lay => lay.Index).ToList();

            foreach (var index in roadCLLayerIndices)
            {
                var roadCLs = doc.Objects.Where(o => o.Attributes.LayerIndex == index).ToList();

                if (!Double.TryParse(doc.Layers[index].Name, out double roadWidth))
                    continue;

                roadWidth = roadWidth.Round(CoreGeneralConstants.TransitRouteWidthPrecision);

                List<(BH.oM.Geometry.Polyline?, Guid)> cLAndIds = new();
                foreach (var obj in roadCLs)
                {
                    if (TryExtractBHoMPolyline(obj, out BH.oM.Geometry.Polyline? pline))
                        cLAndIds.Add((pline!, obj.Id));
                }

                ModelBuilderPipelines.AddOrUpdateRoadPipeline(doc, cLAndIds, roadWidth);
            }
        }

        internal static void ResetStudyAreas()
        {
            CoreModel.ResetStudyAreas();
        }
    }
}
