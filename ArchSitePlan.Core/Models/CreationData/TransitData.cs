using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models.CreationData
{
    public class TransitData
    {
        public ICurve Centerline;
        public Guid ID;
        public double RoadWidth;

        public TransitData(ICurve centerline, Guid id, double roadWidth)
        {
            Centerline = centerline;
            ID = id;
            RoadWidth = roadWidth;
        }
    }
}
