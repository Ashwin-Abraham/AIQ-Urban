using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.Models
{
    public class RhinoDividerData
    {
        public Curve Curve;
        public Guid ID { get; }

        private RhinoDividerData(Curve curve, Guid id)
        {
            Curve = curve;
            ID = id;
        }

        private static bool ValidateInput(RhinoObject obj)
        {
            return obj.Geometry is Curve rhinoCrv
                && rhinoCrv.Degree > 2;
        }

        public static IEnumerable<RhinoDividerData> Create(IEnumerable<RhinoObject>  objs)
        {
            return objs.Select(obj => Create(obj))
                .Where(x => x != null)
                .Cast<RhinoDividerData>();
        }
        public static RhinoDividerData? Create(RhinoObject obj)
        {
            if(!ValidateInput(obj)) 
                return null;

            return new RhinoDividerData((obj.Geometry as Curve)!, obj.Id);
        }

        public static IEnumerable<DividerData> ToCoreModelData(IEnumerable<RhinoDividerData> rhinoData)
        {
            return rhinoData.Select(d => d.ToCoreModelData());
        }

        public DividerData ToCoreModelData()
        {
            return new DividerData(
                Curve.ToBHoM2DegreeCrv()!,
                ID);
        }

      
    }
}
