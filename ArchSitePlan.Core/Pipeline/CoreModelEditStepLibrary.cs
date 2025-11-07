using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Factories.Models;
using ArchSitePlan.Core.ModelEditActions;
using ArchSitePlan.Core.ModelEditActions.Configs;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.Pipeline;

public static class CoreModelEditStepLibrary
{
    public static void EditParcelUsesByClosestPoint(
        List<(Point point, LanduseData use)> landuseData)
    {
        List<(Point point, Landuse use)> uses = landuseData
            .Select(x => (x.point, CoreModelSingelton.Instance.GetUse(x.use)))
            .ToList();

        if (uses.Count == 0) return;

        foreach (var p in CoreModelSingelton.Instance.Parcels)
        {
            var closestRhinoData = uses.MinBy(data => data.point.Distance(p.Boundary.ControlPointCentroid()));
            p.LandUse = closestRhinoData.use;
        }
    }
    public static void ClearCoreModel() => CoreModelSingelton.Instance.ClearModel();
    
    /// <summary>
    /// Updates the floorplate use if it exists
    /// </summary>
    public static void UpdateFloorplateUse(List<Guid> clientModelIDs, string useName)
    {
        var use = CoreModelSingelton.Instance.Landuses.FirstOrDefault(l => l.Name == useName);

        if (use == null)
            return;

        var floorplateData = clientModelIDs
            .Select(id => (id, use))
            .ToList();

        new FloorplateEditUseAction().Run(floorplateData);
    }

    /// <summary>
    /// Updates the parcel use if it exists
    /// </summary>
    public static void UpdateParcelUse(List<Guid> clientModelIDs, string use)
    {
        new ParcelAction().Run(
            editType: EditType.Edit,
            clientIdsToModifyOrRemove: clientModelIDs, 
            useName: use);
    }

    /// <summary>
    /// Updates the parcel use if it exists
    /// </summary>
    public static void EnsureEachFloorplateHasUse()
    {
        new AssignUseToFloorplatesWithoutUse().Run();
    }

    public static bool TryRemoveRoad(IEnumerable<TransitData> transitData)
    {
        int initialRoadCount = CoreModelSingelton.Instance.Roads.Count;
        RoadAction.Run(transitData.ToList(), EditType.Remove);
        return initialRoadCount > CoreModelSingelton.Instance.Roads.Count;
    }

    public static void DivideParcelsAndTransit()
    {
        var dividers = CoreModelSingelton.Instance.Dividers
            .Select(d => d.Curve)
            .Concat(CoreModelSingelton.Instance.Zones.Select(s => s.Boundary))
            .Where(x => x != null)
            .Cast<ICurve>()
            .ToList();

        if (dividers.Count == 0) return;

        new ParcelAction().Run(
            clientIdsToModifyOrRemove: CoreModelSingelton.Instance.Parcels.Select(p => p.ClientModelId).ToList(),
            editType: EditType.Divide,
            dividers: dividers);
    }

    public static void AddOrUpdateRoad( List<TransitData> transitData)
    {
        RoadAction.Run(transitData, EditType.Add);
    }
    public static void RecalculateParcelsAndRoadRoWs()
    {
        var priorUsesAndCenters = GetPriorUses(CoreModelSingelton.Instance.Parcels);

        // Recalculate Parcels
        CoreModelSingelton.Instance.Parcels.Clear();
        CoreModelSingelton.Instance.ROWs.Clear();

        CalculateParcelAndRoadBoundaries(
           CoreModelSingelton.Instance.Roads,
           out List<PlanarSurface> roadSrfs,
           out List<PlanarSurface> parcelSrfs);

        var roadUnion = roadSrfs.Select(x => x.ExternalBoundary)
            .BooleanUnion()
            .ToList();

        var studiesCombined = CoreModelSingelton.Instance.Zones
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
        roadSrfs = roads
            .Select(x => x.RoW)
            .Where(x => x!=null)
            .Cast<PlanarSurface>()
            .ToList()
            .BooleanUnion();

        var parcelCrvs = roadSrfs.SelectMany(x => x.InternalBoundaries).ToList();
        
        var roadExternals = roadSrfs.Select(x => x.ExternalBoundary).ToList();

        parcelSrfs = new();

        foreach(var parcelCrv in parcelCrvs)
        {
            var roadsWithin = roadExternals.Where(x => parcelCrv.IIsContaining(x)).ToList();

            if(roadsWithin.Count == 0)
            {
                parcelSrfs.Add(Create.PlanarSurface(parcelCrv));
            }
            else
            {
                var remainingSrf = Create.PlanarSurface(parcelCrv).BooleanDifference(roadsWithin);
                parcelSrfs.AddRange(remainingSrf);
            }
        }
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
        int initialCount = CoreModelSingelton.Instance.Dividers.Count;
        new DividerAction().Run(dividerData, EditType.Remove);

        return CoreModelSingelton.Instance.Dividers.Count < initialCount;
    }

    public static void AddDivders(List<DividerData> dividerData) =>
        new DividerAction().Run(dividerData, EditType.Add);

    public static void AddOrUpdateBulding(List<Building> buildings)
    {
        CoreModelSingelton.Instance.Buildings.AddRange(buildings);
    }

    public static void AddOrUpdateBulding(List<BuildingData> buildingData)
    {
        AddBuildingAction.Run(buildingData);
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

    public static void RemoveBuildingsOnParcels(List<BuildingLayout> layoutsToAdd)
    {
        var hostParcelIds = layoutsToAdd.Select(x => x.HostParcelCoreModelId);

        var buildingsToDelete = CoreModelSingelton.Instance.Buildings
            .Where(x => hostParcelIds.Contains(x.HostParcelCoreModelId))
            .ToList();

        buildingsToDelete.ForEach(x => CoreModelSingelton.Instance.Buildings.Remove(x));
    }

    public static void StoreHistory()
    {
        HistoryAction.StoreState();
    }

    public static void SetLanduses(List<LanduseData> landuseData)
    {
        LanduseActions.Run(landuseData, EditType.ClearAll);
        LanduseActions.Run(landuseData, EditType.Edit);
    }
}
