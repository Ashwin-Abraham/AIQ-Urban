using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Data;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using static ArchSitePlan.Core.Constants.Enums;
using Polyline = Rhino.Geometry.Polyline;
namespace ArchSitePlan.UI.Actions
{
    public static class RhinoToCoreModel
    {
        public static void AddRoad(RhinoObject obj, double width)
        {
            Guid id = obj.Id;
            Curve curve = obj.Geometry as Curve;

            if (curve.Degree > 2)
                throw new Exception("Curve degree is too high, please select line, polyline or arc only");

            if (curve.TryGetPolyline(out Polyline polyline))
            {
                var bhom = polyline.ToBHoM();
                CoreModel.AddOrUpdateRoads((bhom, id, width).ObjectToList());
            }
        }

        public static void UpdateRoadNetwork(List<RhinoObject> modifiedRhinoObjs)
        {
            var roadCenterlineGuids = CoreModel.Roads
                .Select(r => r.CenterLineId)
                .ToList();

            if (!roadCenterlineGuids.Any()) return;

            var roadCrvs = modifiedRhinoObjs
                .Where(obj => obj != null && roadCenterlineGuids.Contains(obj.Id))
                .Select(obj =>
                {
                    var curve = obj.Geometry as Curve;
                    if (curve != null && curve.TryGetPolyline(out Polyline polyline))
                    {
                        var bhom = polyline.ToBHoM();
                        return (bhom, obj.Id);
                    }

                    return (null, Guid.Empty);
                })
                .Where(x => x.bhom != null)
                .ToList();

            if (CoreModel.TryUpdateSomeRoads(roadCrvs))
            {
                roadCenterlineGuids = CoreModel.Roads
               .Select(r => r.CenterLineId)
               .ToList();
            }
        }

        /// <summary>
        /// Updates the parcel use if it exists
        /// </summary>
        public static void TryUpdateParcelUse(RhinoObject parcel, Landuse use)
        {
            CoreModel.UpdateParcelUse(parcel.Id.ObjectToList(), use);
        }

        internal static bool TryGetStudyAreaAndMetric(Guid id, out StudyArea area)
        {
           return CoreModel.TryGetStudyArea(id, out area);
        }

        internal static bool TryAddStudyArea(BH.oM.Geometry.Polyline boundary, 
            Guid id, 
            string name)
            => CoreModel.TryAddOrUpdateStudyArea(boundary, id, name);

        internal static bool TryFindRoad(Guid id, out Road road)
            => CoreModel.TryFindRoad(id,out road);

        internal static void AddOrUpdateRoads(List<(BH.oM.Geometry.Polyline, Guid, double)> plineObjs)
            => CoreModel.AddOrUpdateRoads(plineObjs);

        internal static void TryRemoveBuilding(Guid objectId)
        {
            CoreModel.TryRemoveBuilding(objectId);
        }

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

            // PARCELS
            // Read parcel Objects and store center points
            var parcelLayIndices = LayerHelpers
                .GetParcelRhinoLayerNames()
                .Select(name => doc.Layers.FindByFullPath(name, -1))
                .ToList();

            var rhinoParcelCenters = doc.Objects
                .Where(obj => parcelLayIndices.Contains(obj.Attributes.LayerIndex)
                    && obj.ObjectType == ObjectType.Surface)
                .Select(srf => (srf.Geometry as Surface))
                .Select(srf => VolumeMassProperties.Compute(srf).Centroid);

            // TRANSIT
            ReloadTransitFromRhino(doc);

            // STUDY AREAS
            // Read study area objects
            CoreModel.ResetStudyAreas();

            // BUILDINGS
            // Read building objects
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

                roadCLs.ForEach(obj => AddRoad(obj, roadWidth));
            }
        }

        internal static void ResetStudyAreas()
        {
            CoreModel.ResetStudyAreas();
        }

        internal static bool TryRemoveRoad(Guid id)
        {
            return CoreModel.TryRemoveRoad(id);
        }
    }
}
