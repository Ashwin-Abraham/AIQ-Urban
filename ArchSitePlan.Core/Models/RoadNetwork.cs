using ArchSitePlan.Core.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models
{
    public class RoadNetwork
    {
        private List<Road> _roads  = new List<Road>();
        public IReadOnlyList<Road> Roads => _roads.AsReadOnly();
        public List<PlanarSurface> ROWs => CalculateRoW();
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

        private List<PlanarSurface> CalculateRoW()
        {
            var externalBoundaries = _roads.Select(r => r.RoW.ExternalBoundary);
            var union = Compute.BooleanUnion(externalBoundaries)
                .Select(pCrv => Create.PlanarSurface(pCrv))
                .ToList();

            return union;
        }

        public void AddRoad(Polyline centerLine, Guid id, double width)
        {
            var pCurve = Create.PolyCurve(centerLine.ObjectToList());
            if(Road.TryCreateRoad(pCurve, id, width, out Road road))
            {
                TryRemove(road.CenterLineId);
                _roads.Add(road);
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
