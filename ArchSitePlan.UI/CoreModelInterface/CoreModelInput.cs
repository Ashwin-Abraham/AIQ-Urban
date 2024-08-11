using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino.DocObjects;
using Rhino.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

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
                CoreModel.AddRoad(polyline.ToBHoM(), id, width);
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
            CoreModel.Parcels.FindAll(p => p.Id == parcel.Id)
                .ForEach(p => p.LandUse = use);
        }
    }
}
