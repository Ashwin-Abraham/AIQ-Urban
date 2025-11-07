using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;

namespace ArchSitePlan.Core.Metrics
{
    public class TransitMetrics
    {
        public double RoadWidth { get; private set; }
        public double RunningLength { get; private set; }
        public double Area { get; private set; }

        public TransitMetrics(Road road)
        {
            RoadWidth = road.Width;
            RunningLength = road.Centerline.Length();
            Area = RunningLength * RoadWidth;
        }
    }
}
