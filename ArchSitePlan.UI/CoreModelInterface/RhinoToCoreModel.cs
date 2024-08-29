using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Rhino.DocObjects;
using Rhino.Geometry;
using static ArchSitePlan.Core.Constants.Enums;
using Polyline = Rhino.Geometry.Polyline;
namespace ArchSitePlan.UI.Actions
{
    public static class RhinoToCoreModel
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
                CoreModel.AddRoad(bhom, id, width);
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
            CoreModel.UpdateParcelUse(parcel.Id.ObjectToList(), use);
        }

        internal static bool TryGetStudyArea(Guid id, out StudyArea area)
        {
           return CoreModel.TryGetStudyArea(id, out area);
        }

        internal static bool TryAddStudyArea(BH.oM.Geometry.Polyline boundary, 
            Guid id, 
            string name)
        {
            return CoreModel.TryAddStudyArea(boundary, id, name);
        }
    }
}
