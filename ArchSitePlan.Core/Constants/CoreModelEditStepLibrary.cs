using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.ModelEditActions;
using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;
using static ArchSitePlan.Core.ModelEditActions.BuildingAction;

namespace ArchSitePlan.Core.Constants
{
    public record StudyCreationData(
            BH.oM.Geometry.Polyline Boundary,
            Guid Id,
            string Name);

    public record TransitCreationData(
        BH.oM.Geometry.Polyline? Centerline,
        Guid Id,
        double RoadWidth);

    public record BuildingCreationData(
        Mesh? Envelope, 
        List<PlanarSurface> Floors, 
        Guid EnvelopeId, 
        Guid FloorId, 
        double FloorHt);

    public record DividerCreationData(
        Polyline? Polyline, 
        Guid Id);
    public class CoreModelEditStepLibrary
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
        public static void AddOrUpdateRoad(List<TransitCreationData> transitData)
        {
            var groupedByWidths = transitData.GroupBy(data => data.RoadWidth);
            foreach(var group in groupedByWidths)
            {
                List<(Polyline? centerLine, Guid id)> centerLineAndIds = group
                    .AsEnumerable()
                    .Select(g => (g.Centerline, g.Id))
                    .ToList();

                AddOrUpdateRoad(centerLineAndIds, group.Key);
            }
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

            var studyBoundaries = CoreModel.Studies
                .Where(s => s.Boundary != null)
                .Select(s => s.Boundary)
                .ToList();

            var studiesCombined = Compute.BooleanUnion(studyBoundaries);

            var unenclosedParcels = studiesCombined.
                SelectMany(crv => Compute.BooleanDifference(crv, roadUnion))
                .ToList();

            var roadSrfs = roadCrvs
                .Select(r => Create.PlanarSurface(
                    r,
                    parcelCrvs.Cast<ICurve>().ToList()))
                .ToList();

            parcelBoundaries = parcelCrvs.Concat(unenclosedParcels).ToList();
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

        public static void AddDivders(List<DividerCreationData> divData)
        {
            AddDivders(
                divData
                .Select(d => (d.Polyline, d.Id))
                .ToList()
                );
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

        public static void AddOrUpdateStudyArea(List<StudyCreationData> studyData) =>
            AddOrUpdateStudyArea(
                studyData.Select(d => (d.Boundary, d.Id, d.Name)).ToList());

        public static void AddOrUpdateStudyArea(List<(Polyline?, Guid, string)> studyData) =>
            StudyAreaAction.Run(studyData, EditType.Edit);

        public static void DeleteStudyAreas(List<Guid> guids)
        {
            List<(Polyline?, Guid, string)> studyData = new();
            guids.ForEach(id => studyData.Add((null, id, string.Empty)));
            StudyAreaAction.Run(studyData, EditType.Remove);
        }

        public static List<(Polyline, Guid, string)> BuildStudyDataForValidStudies(
            List<(Polyline Crv, Guid Id)> plineObjs)
        {
            var ids = CoreModel.Studies.Select(s => s.Id).ToList();
            var studyObjs = new List<(Polyline, Guid, string)>();

            foreach (var pline in plineObjs)
            {
                if (CoreModel.TryGetStudyArea(pline.Id, out StudyArea study))
                    studyObjs.Add((pline.Crv, pline.Id, study.Name));
            }
            return studyObjs;
        }

        public static void AddOrUpdateBulding(List<BuildingCreationData> buildingData)
        {
            buildingData.ForEach(data =>AddOrUpdateBulding(
                (data.Envelope, data.EnvelopeId, data.Floors).ObjectToList(),
                data.FloorHt,
                data.Floors.Count));
        }
    }
}
