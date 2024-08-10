using ArchSitePlan.Core.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models
{
    public class RoadNetwork
    {
        private List<Road> _roads  = new List<Road>();
        public IReadOnlyList<Road> Roads => _roads.AsReadOnly();
        public List<PlanarSurface> ROWs = new List<PlanarSurface>();
        public RoadNetwork() { }

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

        public bool TryUpdateSomeRoads(List<(Polyline,Guid)> newGuidAndCL)
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

        public bool TryRemoveRoad(Guid roadCLId)
        {
            int count = _roads.RemoveAll(r => r.CenterLineId == roadCLId);
            return count > 0;
        }

        private void UpdateRoWs()
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

            ROWs = roadSrfs;
        }

        public void AddRoad(Polyline centerLine, Guid id, double width)
        {
            var pCurve = Create.PolyCurve(centerLine.ObjectToList());
            if(Road.TryCreateRoad(pCurve, id, width, out Road road))
            {
                TryRemove(road.CenterLineId);
                _roads.Add(road);
                UpdateRoWs();
            }
        }

        public bool TryRemove(Guid roadId)
        {
            int removalIndex = _roads.FindIndex(r => r.CenterLineId.Equals(roadId));

            if (removalIndex != -1)
            {
                _roads.RemoveAt(removalIndex);
                return true;
            }

            return false;
        }
    }
}
