using ArchSitePlan.Core.Singletons;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Actions
{
    public static class CoreModelInput
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
                Singletons.RoadNetwork.AddRoad(polyline.ToBHoM(), id, width);
            }
        }

        public static void UpdateRoadNetwork(List<RhinoObject> modifiedRhinoObjs)
        {
            var roadCenterlineGuids = Singletons.RoadNetwork.Roads
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

            if (Singletons.RoadNetwork.TryUpdateSomeRoads(roadCrvs))
            {
                roadCenterlineGuids = Singletons.RoadNetwork.Roads
               .Select(r => r.CenterLineId)
               .ToList();
            }
        }
    }
}
