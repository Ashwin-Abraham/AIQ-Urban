using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Factories.Models;
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
        public static void UpdateParcelUse(List<Guid> clientModelIDs, Landuse use)
        {
            new ParcelAction().Run(
                editType: EditType.Edit,
                clientIdsToModifyOrRemove: clientModelIDs, 
                use: use);
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
                .Concat(CoreModel.Studies.Select(s => s.Boundary))
                .Where(x => x != null)
                .Cast<ICurve>()
                .ToList();

            if (dividers.Count == 0) return;

            new ParcelAction().Run(
                clientIdsToModifyOrRemove: CoreModel.Parcels.Select(p => p.ClientModelId).ToList(),
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
               CoreModel.Roads,
               out List<PlanarSurface> roadSrfs,
               out List<PlanarSurface> parcelSrfs);

            var roadUnion = roadSrfs.Select(x => x.ExternalBoundary)
                .BooleanUnion()
                .ToList();

            var studiesCombined = CoreModel.Studies
                .Where(s => s.Boundary != null)
                .Select(s => s.Boundary)
                .BooleanUnion()
                .ToList();

            var unenclosedParcels = studiesCombined.
                SelectMany(crv => crv.BooleanDifference(roadUnion))
                .Select(x => new PlanarSurface(x, []))
                .ToList();

            new ParcelAction().Run(
                editType: EditType.Add,
                parcelsToAdd: parcelSrfs.Concat(unenclosedParcels).ToList(),
                existingUsesAndCenters: priorUsesAndCenters);

            RoadAction.Run(roadSrfs);
        }

        private static List<(Landuse, Point)>? GetPriorUses(List<Parcel> parcels)
          => parcels.Select(p => (p.LandUse, p.Boundary.ControlPointCentroid())).ToList();

        public static void CalculateParcelAndRoadBoundaries(
            List<Road> roads,
            out List<PlanarSurface> roadSrfs,
            out List<PlanarSurface> parcelSrfs)
        {
            var inputSrfs = roads.Select(x => x.RoW).ToList().BooleanUnion();

            roadSrfs = new List<PlanarSurface>();
            parcelSrfs = new List<PlanarSurface>();
        }

        public static void CalculateParcelAndRoadBoundaries2(
            List<Road> roads,
            out List<PlanarSurface> roadSrfs,
            out List<PlanarSurface> parcelSrfs)
        {
            var validRoads = roads.Where(r => r.IsValid()).ToList();
            var roadOuterBoundaries = validRoads.Select(r => r.RoW!.ExternalBoundary).ToList();
            var roadInnerBoundaries = validRoads
                .SelectMany(r => r.RoW!.InternalBoundaries)
                .ToList();

            List<ICurve> innerBoundaries = new();
            foreach (var innerBoundary in roadInnerBoundaries)
            {
                var clashingRoads = validRoads
                    .Where(b => b.RoW!.ExternalBoundary.ICurveIntersections(innerBoundary).Count > 0)
                    .ToList();

                if (clashingRoads.Count > 0)
                {
                    var clashingExternals = clashingRoads.Select(x => x.RoW.ExternalBoundary).ToList();
                    var clashingInternals = clashingRoads.SelectMany(x => x.RoW.InternalBoundaries).ToList();

                    var reducedInnerBoundary = innerBoundary.BooleanDifference(clashingExternals).ToList();
                    var overlappingInners = clashingInternals.SelectMany(x=> x.BooleanIntersection(innerBoundary)).ToList();
                    var newInnerBoundaries = reducedInnerBoundary.Concat(overlappingInners?? []).BooleanUnion();
                    
                    innerBoundaries.AddRange(newInnerBoundaries);
                }
                else
                    innerBoundaries.Add(innerBoundary);
            }

            // This union gets rid of overlapping innerboundaries generated by 2 different roads
            roadInnerBoundaries = innerBoundaries.BooleanUnion().Cast<ICurve>().ToList();

            var roadUnion = roadOuterBoundaries.BooleanUnion()
                .ToList();

            // Find roads that do not touch the result, meaning that these were swallowed up by the union.
            // Re-introduce them as roads.
            var roadCrvs = roadUnion
                .Select(x => (
                    Boundary: x,
                    ParentRoad: FindOneRoadThatCreatedThisBoundary(x, roads)))
                .Where(x => 
                    x.ParentRoad != null
                    && x.Boundary.IIsContaining([x.ParentRoad.Centerline.Mid()]))
                .Select(x => x.Boundary)
                .ToList();

            var parcelCrvs = roadUnion
                .Where(pCrv => !roadCrvs.Contains(pCrv))
                .Where(crv => crv is not null)
                .Cast<ICurve>()
                .ToList();

            parcelCrvs.AddRange(roadInnerBoundaries);

            roadSrfs = roadCrvs
                .Select(r => Create.PlanarSurface(
                    r,
                    parcelCrvs))
                .ToList();

            
            parcelSrfs = parcelCrvs
                .Select(x => new PlanarSurface(x, CurvesContainedBy(x, roadOuterBoundaries)))
                .ToList();

            var remainingRoads = roads
                .Where(x => IsWithinAnyCurve(x.RoW.ExternalBoundary, parcelCrvs))
                .ToList();

            if (remainingRoads.Count > 0)
            {
                CalculateParcelAndRoadBoundaries(remainingRoads, out var roadSrfsWithin, out var parcelSrfsWithin);
                roadSrfs.AddRange(roadSrfsWithin);
                parcelSrfs.AddRange(parcelSrfsWithin);
            }
        }

        private static bool IsWithinAnyCurve(ICurve curve, List<ICurve> containmentCurves)
        {
            return containmentCurves.Exists(x => x.IIsContaining(curve));
        }

        private static List<ICurve> CurvesContainedBy(ICurve curve, List<ICurve> curvesToCheck)
        {
            List<ICurve> curvesWithin = new();

            foreach(var curveBeingChecked in curvesToCheck)
            {
                if (curve.IIsContaining(curveBeingChecked))
                    curvesWithin.Add(curveBeingChecked);
            }

            return curvesWithin;
        }

        /// <summary>
        /// Find one road that the boundary (partially) came from
        /// </summary>
        private static Road? FindOneRoadThatCreatedThisBoundary(ICurve boundary, List<Road> roads)
        {
            foreach(var road in roads)
            {
                var testPoints = road.RoW.ExternalBoundary.ISubParts().Select(x => x.Mid()).ToList();
                var pointOnBoundary = testPoints.Find(x => x.IDistance(boundary) < 1e-06);

                if(pointOnBoundary != null)
                    return road;
            }

            return null;
        }

        public static bool TryRemoveDivders(List<DividerData> dividerData)
        {
            int initialCount = CoreModel.Dividers.Count;
            new DividerAction().Run(dividerData, EditType.Remove);

            return CoreModel.Dividers.Count < initialCount;
        }

        public static void AddDivders(List<DividerData> dividerData) =>
            new DividerAction().Run(dividerData, EditType.Add);

        public static void AddOrUpdateBulding(List<Building> buildings)
        {
            CoreModel.Buildings.AddRange(buildings);
        }
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

        public static void RemoveBuildingsOnParcels(List<BuildingLayout> layoutsToAdd)
        {
            var pointsOnParcel = layoutsToAdd
                .Select(x => x.SiteBoundary.IControlPoints())
                .ToList();

            var hostParcelIds = new List<Guid>();

            foreach (var parcel in CoreModel.Parcels)
            {
                foreach(var layoutParcelPoints in pointsOnParcel)
                {
                    bool foundParcel = parcel.Boundary.ExternalBoundary.IIsContaining(layoutParcelPoints);
                    if (!foundParcel) continue;
                    
                    hostParcelIds.Add(parcel.CoreModelId);
                }
            }
            var buildingsToDelete = CoreModel.Buildings
                .Where(x => hostParcelIds.Contains(x.HostParcelCoreModelId))
                .ToList();

            buildingsToDelete.ForEach(x => CoreModel.Buildings.Remove(x));
        }
    }
}
