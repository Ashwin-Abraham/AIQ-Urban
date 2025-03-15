using AIQ.Primitives.Extensions;
using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.RhinoInterface.RhinoBHoMInterop;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace ArchSitePlan.RhinoInterface.Models
{
    public class RhinoRoadData
    {
        public Curve Centerline;
        public Guid ID;
        public double RoadWidth;

        private RhinoRoadData(Curve centerline, Guid id, double roadWidth)
        {
            Centerline = centerline;
            ID = id;
            RoadWidth = roadWidth;
        }

        public static RhinoRoadData? Create(RhinoObject obj, double roadWidth)
        {
            if(roadWidth.AlmostZero() || obj.Geometry is not Curve rhinoCrv) 
                return null;
            if (rhinoCrv.Degree > 2) 
                return null;

            return new RhinoRoadData(rhinoCrv, obj.Id, roadWidth);
        }
        public static IEnumerable<RhinoRoadData> FromExisting(IEnumerable<RhinoObject> objs)
        {
            return objs.Select(x => FromExisting(x))
                .Where(x => x != null)
                .Cast<RhinoRoadData>();
        }

        public static RhinoRoadData? FromExisting(RhinoObject obj)
        {
            if (obj.Geometry is not Curve rhinoCrv) return null;
            if(rhinoCrv.Degree > 2
                && Intersection.CurveSelf(rhinoCrv, 1E-06).Count == 0) 
                return null;

            var road = CoreModel.Roads.Find(b => b.CenterLineId == obj.Id);

            if (road is null) return null;

            return new RhinoRoadData(rhinoCrv, obj.Id, road.Width);
        }

        public static IEnumerable<TransitData> ToCoreModelCreationData(IEnumerable<RhinoRoadData> rhinoData)
        {
            return rhinoData.Select(d => d.ToCoreModelCreationData());
        }
        public TransitData ToCoreModelCreationData()
        {
            return new TransitData(
                Centerline.ToBHoM2DegreeCrv()!,
                ID,
                RoadWidth);
        }
    }
}
