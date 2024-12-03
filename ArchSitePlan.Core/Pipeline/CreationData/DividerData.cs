using BH.oM.Geometry;

namespace ArchSitePlan.Core.Pipeline.CreationData
{
    public class DividerData
    {
        public ICurve Curve;
        public Guid ID;

        public DividerData(ICurve curve, Guid id)
        {
            Curve = curve;
            ID = id;
        }
    }
}
