using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.ModelEditActions;
using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;
using static ArchSitePlan.Core.ModelEditActions.BuildingAction;

namespace ArchSitePlan.Core.Constants
{
    public class CoreModelEditStepLibrary
    {
        //public record ModelEditStep(
        //   EditType EditMode,
        //   List<IModelEditAction> ActionSequence);
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

        public static bool TryRemoveRoad(IEnumerable<Guid> ids)
        {
            int initialRoadCount = CoreModel.Roads.Count;
            List<(Polyline?, Guid)> centerLineAndIds = new();
            foreach (var id in ids)
                centerLineAndIds.Add((null, id));

            RoadAction.Run(centerLineAndIds, EditType.Remove, null);

            return initialRoadCount > CoreModel.Roads.Count;
        }

        public static void DivideParcels()
        {
            var dividers = CoreModel.Dividers
                .Select(d => d.Curve)
                .Cast<ICurve>()
                .ToList();

            dividers.AddRange(CoreModel.Studies.Select(s => s.Boundary));
            var boundaryAndIds = CoreModel.Parcels
                .Select(p => (p.Boundary.ExternalBoundary, p.OriginalObjectID))
                .ToList();

            new ParcelAction().Run(
                boundaryAndIds,
                editType: EditType.Divide,
                dividers: dividers);
        }
        public static void AddOrUpdateRoad(
           List<(Polyline? centerLine, Guid id)> centerLineAndIds,
           double width)
        {
            RoadAction.Run(centerLineAndIds, EditType.Add, width);
        }
        public static void RecalculateParcelsAndRoadRoWs()
        {
            var priorUsesAndCenters = GetPriorUses(CoreModel.Parcels);

            // Recalculate Parcels
            CoreModel.Parcels.Clear();

            CalculateParcelAndRoadBoundaries(
                out List<PlanarSurface> roadSrfs,
                out List<PolyCurve> parcelBoundaries);

            var parcels = parcelBoundaries.Select(x => (x as ICurve, Guid.Empty)).ToList();
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
            out List<PolyCurve> parcelBoundaries)
        {
            var roadBoundaries = CoreModel.Roads.Select(r => r.RoW.ExternalBoundary);
            var roadUnion = Compute.BooleanUnion(roadBoundaries)
                .ToList();

            var roadPoints = CoreModel.Roads
                .Select(r => r.Centerline.PointAtParameter(0.5))
                .ToList();

            var roadCrvs = roadUnion
                .Where(pCrv => roadPoints.Any(pt => pCrv.IsContaining(new List<Point>() { pt })))
                .ToList();

            var parcelCrvs = roadUnion
                .Where(pCrv => !roadCrvs.Contains(pCrv))
                .Select(p => p.ToPolycurve())
                .Where(crv => crv is not null)
                .Cast<PolyCurve>()
                .ToList();

            var roadSrfs = roadCrvs
                .Select(r => Create.PlanarSurface(
                    r,
                    parcelCrvs.Cast<ICurve>().ToList()))
                .ToList();

            parcelBoundaries = parcelCrvs ?? new();
            roadRoWs = roadSrfs;
        }

        public static bool TryRemoveDivders(List<Guid> dividerIds)
        {
            int initialCount = CoreModel.Dividers.Count;

            List<(Polyline?, Guid)> dividerData = new();
            dividerIds.ForEach(id => dividerData.Add((null, id)));

            new DividerAction().Run(dividerData, EditType.Remove);

            return CoreModel.Dividers.Count < initialCount;
        }

        public static void AddDivders(List<(Polyline?, Guid)> plineObjs) =>
            new DividerAction().Run(plineObjs, EditType.Add);

        public static void AddOrUpdateBulding(
            List<(Mesh? Envelope, Guid ID, List<PlanarSurface> FloorPlates)> buildingData,
            double ffHt,
            int floorCount)
        {
            var configs = new List<BuildingActionConfig>();

            foreach( var data in buildingData )
            {
                var config = new BuildingActionConfig(
                    data.Envelope,
                    data.ID,
                    Guid.Empty,
                    ffHt,
                    floorCount,
                    data.FloorPlates);

                configs.Add(config);
            }

            BuildingAction.Run(configs, EditType.Add);
        }

        public static bool TryDeleteBuildings(List<Guid> envelopeIds)
        {
            int deleted = CoreModel.Buildings.RemoveAll(bld => envelopeIds.Contains(bld.EnvelopeId));
            return deleted > 0;
        }

        public static void AddOrUpdateStudyArea(List<(Polyline, Guid, string)> studyData) =>
            StudyAreaAction.Run(studyData, EditType.Edit);
    }
}
