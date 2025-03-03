using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.ModelEditActions;
using ArchSitePlan.Core.ModelEditActions.Configs;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.Pipeline
{
    public static class CoreModelEditStepLibrary
    {
        public static void EditParcelUsesByClosestPoint(
            List<(Point point, Landuse use)> landuseData)
        {
            if (landuseData.Count == 0) return;

            foreach (var p in CoreModel.Parcels)
            {
                var closestRhinoData = landuseData.MinBy(data => data.point.Distance(p.Boundary.ControlPointCentroid()));
                p.LandUse = closestRhinoData.use;
            }
        }
        public static void ClearCoreModel() => CoreModel.ClearModel();
        public static void RefreshStudyAreaMetrics()
        {
            CoreModel.RefreshStudyAreaMetrics();
        }
        /// <summary>
        /// Updates the parcel use if it exists
        /// </summary>
        public static void UpdateParcelUse(List<Guid> parcelIds, Landuse use)
        {
            List<(ICurve?, Guid)> parcelsAndIds = new();
            foreach (Guid parcelId in parcelIds)
            {
                parcelsAndIds.Add((null, parcelId));
            }

            new ParcelAction().Run(parcelsAndIds, EditType.Edit, use);
        }

        public static bool TryRemoveRoad(IEnumerable<TransitData> transitData)
        {
            int initialRoadCount = CoreModel.Roads.Count;
            RoadAction.Run(transitData.ToList(), EditType.Remove);
            return initialRoadCount > CoreModel.Roads.Count;
        }

        public static void DivideParcelsAndTransit()
        {
            var dividers = CoreModel.Dividers
                .Select(d => d.Curve)
                .Cast<ICurve>()
                .ToList();

            dividers.AddRange(CoreModel.Studies.Select(s => s.Boundary));
            dividers.RemoveAll(x => x is null);

            if (dividers.Count == 0) return;

            var parcelBoundaryAndIds = CoreModel.Parcels
                .Select(p => (p.Boundary.ExternalBoundary, p.Id))
                .ToList();

            new ParcelAction().Run(
                parcelBoundaryAndIds,
                editType: EditType.Divide,
                dividers: dividers);
        }

        public static void AddOrUpdateRoad( List<TransitData> transitData)
        {
            RoadAction.Run(transitData, EditType.Add);
        }
        public static void RecalculateParcelsAndRoadRoWs()
        {
            var priorUsesAndCenters = GetPriorUses(CoreModel.Parcels);

            // Recalculate Parcels
            CoreModel.Parcels.Clear();
            CoreModel.ROWs.Clear();

            CalculateParcelAndRoadBoundaries(
                out List<PlanarSurface> roadSrfs,
                out List<ICurve> parcelBoundaries);

            var parcels = parcelBoundaries.Select(x => (x, Guid.Empty)).ToList();
            new ParcelAction().Run(
                editType: EditType.Add,
                boundaryAndIds: parcels,
                dividers: null,
                    existingUsesAndCenters: priorUsesAndCenters);

            RoadAction.Run(roadSrfs);
        }

        private static List<(Landuse, Point)>? GetPriorUses(List<Parcel> parcels)
          => parcels.Select(p => (p.LandUse, p.Boundary.ControlPointCentroid())).ToList();

        private static void CalculateParcelAndRoadBoundaries(
            out List<PlanarSurface> roadRoWs,
            out List<ICurve> parcelBoundaries)
        {
            var validRoads = CoreModel.Roads.Where(r => r.IsValid()).ToList();
            var roadOuterBoundaries = validRoads.Select(r => r.RoW!.ExternalBoundary);
            var roadInnerBoundaries = validRoads
                .SelectMany(r => r.RoW!.InternalBoundaries)
                .ToList();

            List<ICurve> innerBoundaries = new();
            foreach (var innerBoundary in roadInnerBoundaries)
            {
                var clash = roadOuterBoundaries
                    .Where(b => b.ICurveIntersections(innerBoundary).Count > 0)
                    .ToList();

                if (clash.Count > 0)
                {
                    var newInnerBoundary = innerBoundary.BooleanDifference(clash).ToList();
                    innerBoundaries.AddRange(newInnerBoundary);
                }
                else
                    innerBoundaries.Add(innerBoundary);
            }
            roadInnerBoundaries = innerBoundaries;

            var roadUnion = roadOuterBoundaries.BooleanUnion()
                .ToList();

            var roadPoints = CoreModel.Roads
                .Select(r => r.Centerline.PointAtParameter(0.5))
                .ToList();

            var roadCrvs = roadUnion
                .Where(pCrv => roadPoints.Any(pt => pCrv.IsContaining(new List<Point>() { pt })))
                .ToList();

            var parcelCrvs = roadUnion
                .Where(pCrv => !roadCrvs.Contains(pCrv))
                .Where(crv => crv is not null)
                .Cast<ICurve>()
                .ToList();

            parcelCrvs.AddRange(roadInnerBoundaries);

            var studyBoundaries = CoreModel.Studies
                .Where(s => s.Boundary != null)
                .Select(s => s.Boundary)
                .ToList();

            var studiesCombined = studyBoundaries.BooleanUnion();

            var unenclosedParcels = studiesCombined.
                SelectMany(crv => crv.BooleanDifference(roadUnion))
                .ToList();

            var roadSrfs = roadCrvs
                .Select(r => Create.PlanarSurface(
                    r,
                    parcelCrvs))
                .ToList();

            parcelBoundaries = parcelCrvs.Concat(unenclosedParcels).ToList();
            roadRoWs = roadSrfs;
        }

        public static bool TryRemoveDivders(List<DividerData> dividerData)
        {
            int initialCount = CoreModel.Dividers.Count;
            new DividerAction().Run(dividerData, EditType.Remove);

            return CoreModel.Dividers.Count < initialCount;
        }

        public static void AddDivders(List<DividerData> dividerData) =>
            new DividerAction().Run(dividerData, EditType.Add);

        public static void AddOrUpdateBulding(List<BuildingData> buildingData)
        {
            var configs = new List<BuildingActionConfig>();

            foreach (var data in buildingData)
            {
                var config = new BuildingActionConfig(
                    data.Envelope,
                    data.EnvelopeId,
                    Guid.Empty,
                    data.FloorHt,
                    data.Floors.Count,
                    data.Floors);

                configs.Add(config);
            }

            BuildingAction.Run(configs, EditType.Add);
        }

        public static int TryDeleteBuildings(List<BuildingData> buildingData)
        {
            var ids = buildingData.Select(x => x.EnvelopeId).ToList();
            return CoreModel.Buildings.RemoveAll(bld => ids.Contains(bld.EnvelopeId));
        }

        public static void AddOrUpdateStudyArea(List<StudyData> studyData) =>
            AddOrUpdateStudyArea(
                studyData.Select(d => (d.Boundary, d.Id, d.Name)).ToList());

        public static void AddOrUpdateStudyArea(List<(ICurve?, Guid, string)> studyData) =>
            StudyAreaAction.Run(studyData, EditType.Edit);

        public static void DeleteStudyAreas(List<Guid> guids)
        {
            List<(ICurve?, Guid, string)> studyData = new();
            guids.ForEach(id => studyData.Add((null, id, string.Empty)));
            StudyAreaAction.Run(studyData, EditType.Remove);
        }

        public static void RefreshBuildingUse()
        {
            var buildingConfigs = CoreModel.Buildings
                .Select(b => new BuildingActionConfig(b))
                .ToList();

            BuildingAction.Run(buildingConfigs, EditType.UpdateUse);
        }

        public static void ReplaceParcelUse(Landuse oldUse, Landuse newUse)
        {
            CoreModel.Parcels
                .Where(x => x.LandUse == oldUse)
                .ToList()
                .ForEach(x => x.LandUse = newUse);
        }

        public static void ReplaceBuildingUse(Landuse oldUse, Landuse newUse)
        {
            CoreModel.Buildings
                .Where(x => x.Use == oldUse)
                .ToList()
                .ForEach(x => x.Use = newUse);
        }
    }
}
