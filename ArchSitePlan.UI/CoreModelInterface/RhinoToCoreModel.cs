using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.ModelEditActions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Data;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using static ArchSitePlan.Core.Constants.Enums;
using Mesh = Rhino.Geometry.Mesh;
using Polyline = Rhino.Geometry.Polyline;
namespace ArchSitePlan.UI.Actions
{
    public static class RhinoToCoreModel
    {
        public static bool TryExtractBHoMPolyline(
            RhinoObject obj,
            out BH.oM.Geometry.Polyline? bhomPline)
        {
            Curve curve = obj.Geometry as Curve;
            bhomPline = null;

            if (curve.Degree > 2)
                throw new Exception("Curve degree is too high, please select line, polyline or arc only");

            if (curve.TryGetPolyline(out Polyline polyline))
            {
                bhomPline = polyline.ToBHoM();
                return bhomPline != null;
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
