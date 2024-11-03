using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino.DocObjects;
using Rhino.Geometry;
using Arc = Rhino.Geometry.Arc;
using Polyline = Rhino.Geometry.Polyline;
namespace ArchSitePlan.UI.Actions
{
    public static class RhinoToCoreModel
    {
        public static bool TryExtractBHoMDeg2Crv(
            RhinoObject obj,
            out BH.oM.Geometry.ICurve? bhomCrv)
        {
            Curve? curve = obj.Geometry! as Curve;
            bhomCrv = null;

            if (curve == null)
                throw new Exception("Not a curve");
            if (curve.Degree > 2)
                throw new Exception("Curve degree is too high, please select line, polyline or arc only");

            if (curve.TryGetPolyline(out Polyline polyline))
            {
                bhomCrv = polyline.ToBHoMPolyline();
                return bhomCrv != null;
            }
            if (curve.TryGetArc(out Arc arc))
            {
                bhomCrv = arc.ToBHoM();
                return bhomCrv != null;
            }

            return false;
        }

        internal static bool TryFindRoad(Guid id, out Road road)
            => CoreModel.TryFindRoad(id,out road);

        internal static List<Parcel> GetParcel(List<Guid> ids)
        {
            if (ids == null) return new List<Parcel>();

            List<Parcel> list = CoreModel.Parcels?
                .Where(parcel => ids.Contains(parcel.OriginalObjectID))
                .ToList();
            
            return list?? new List<Parcel>();
        }

        internal static void RecalculateBuildingHostParcelIDs()
        {
            CoreModel.RecalculateBuildingHostParcelIDs();
        }

        internal static List<Building> FindBuildingsInParcels(List<Parcel> parcels)
        {
            return CoreModel.FindBuildingsInParcels(parcels);
        }
        internal static void ResetStudyAreas()
        {
            CoreModel.ResetStudyAreas();
        }
    }
}
