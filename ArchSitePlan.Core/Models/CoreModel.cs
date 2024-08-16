using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Metrics;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.Models
{
    public static class CoreModel
    {
        private static List<Road> _roads  = new List<Road>();
        public static IReadOnlyList<Road> Roads => _roads.AsReadOnly();
        public static List<PlanarSurface> ROWs = new List<PlanarSurface>();
        public static List<Parcel> Parcels = new List<Parcel>();
        public static ProjectMetrics ProjectMetrics = new ProjectMetrics();
        static CoreModel() { }

        //private void CalculateRoWs()
        //{
        //    List<ICurve> roadRoWsExternalBoundaries = Roads.Select(rd => rd.RoW.ExternalBoundary).ToList();
        //    List<ICurve> roadRoWsVoids = Roads.SelectMany(rd => rd.RoW.InternalBoundaries).ToList();

        //    List<PolyCurve> roWExternalBoundaries = new List<PolyCurve>() { Roads.First().RoW.ExternalBoundary as PolyCurve };
        //    List<PolyCurve> roWInternalBoundaries = new List<PolyCurve>();

        //    roWInternalBoundaries.AddRange(Roads.First().RoW.InternalBoundaries.Cast<PolyCurve>());
            
        //    for(int i = 1; i < Roads.Count; i++)
        //    {
        //        PolyCurve thisCrvExternal = Roads[i].RoW.ExternalBoundary as PolyCurve;
        //        var thisRdVoids = Roads[i]
        //            .RoW.InternalBoundaries
        //            .Cast<PolyCurve>()
        //            .ToList();

        //        List<PolyCurve> thisCrvExternalList = thisCrvExternal.ObjectToList();

        //        roWInternalBoundaries = roWInternalBoundaries
        //            .SelectMany(rd => Compute.BooleanDifference(rd, Roads[i].RoW.ExternalBoundary.ObjectToList()))
        //            .ToList();

        //        roWInternalBoundaries.AddRange(thisRdVoids);
        //        roWExternalBoundaries.Add(thisCrvExternal);
        //    }

        //    roWExternalBoundaries = Compute.BooleanUnion(roWExternalBoundaries);

        //    foreach(PolyCurve externalBoundary in roWExternalBoundaries)
        //    {
        //        var voids = roWInternalBoundaries.Where(crv => externalBoundary.IsContaining(crv)).ToList();
        //        var srf = new PlanarSurface(externalBoundary, voids.Cast<ICurve>().ToList());
        //        ROWs.Add(srf);
        //    }
        //}

        public static bool TryUpdateSomeRoads(List<(Polyline,Guid)> newGuidAndCL)
        {
            var guids = newGuidAndCL.Select(x => x.Item2).ToList();

            var roadsToUpdate = Roads
                .Where(rd => guids.Contains(rd.CenterLineId))
                .ToList();

            foreach (var road in roadsToUpdate )
            {
                var newCL = newGuidAndCL
                    .Find(x => x.Item2 == road.CenterLineId)
                    .Item1;

                AddRoad(newCL, road.CenterLineId, road.Width );
                _roads.Remove(road);
            }

            return roadsToUpdate.Any();
        }

        public static bool TryRemoveRoad(Guid roadCLId)
        {
            int count = _roads.RemoveAll(r => r.CenterLineId == roadCLId);
            return count > 0;
        }

        private static void UpdateParcelsAndRoWs()
        {
            var externalBoundaries = _roads.Select(r => r.RoW.ExternalBoundary);
            var union = Compute.BooleanUnion(externalBoundaries)
                //.Select(pCrv => Create.PlanarSurface(pCrv))
                .ToList();

            var roadPoints = Roads
                .Select(r => r.Centerline.PointAtParameter(0.5))
                .ToList();

            var roads = union
                .Where(pCrv => pCrv.IsContainingAny(roadPoints))
                .ToList();

            var parcels = union.Where(pCrv => !roads.Contains(pCrv))
                .Cast<ICurve>()
                .ToList();

            var roadSrfs = roads.Select(r => Create.PlanarSurface(r, parcels)).ToList();

            Parcels = union.Where(pCrv => !roads.Contains(pCrv))
                .Select(p => new Parcel(Create.PlanarSurface(p)))
                .ToList();

            ROWs = roadSrfs;
        }

        public static void AddRoad(Polyline centerLine, Guid id, double width)
        {
            var pCurve = Create.PolyCurve(centerLine.ObjectToList());
            if(Road.TryCreateRoad(pCurve, id, width, out Road road))
            {
                TryRemove(road.CenterLineId);
                _roads.Add(road);
                UpdateParcelsAndRoWs();
                UpdateParcelMetrics();
            }
        }

        private static void UpdateParcelMetrics()
        {
            ProjectMetrics.UpdateParcelMetrics(Parcels);
        }

        public static bool TryRemove(Guid roadId)
        {
            int removalIndex = _roads.FindIndex(r => r.CenterLineId.Equals(roadId));

            if (removalIndex != -1)
            {
                _roads.RemoveAt(removalIndex);
                return true;
            }

            return false;
        }

        public static void UpdateParcelUse(List<Guid> guids, Landuse use)
        {
            Parcels.FindAll(p => guids.Contains(p.Id))
            .ForEach(p => p.LandUse = use);
            ProjectMetrics.UpdateParcelMetrics(Parcels);
        }

        public static IEnumerable<object> GetRoads()
        {
            throw new NotImplementedException();
        }
    }
}
