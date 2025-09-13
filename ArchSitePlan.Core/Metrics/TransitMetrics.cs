using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.Metrics
{
    public class TransitMetrics
    {
        public double RoadWidth { get; private set; }
        public double RunningLength { get; private set; }
        public TransitType Type { get; private set; }
        public double Area { get; private set; }

        public TransitMetrics(IGrouping<double, Road> transitGroupedByWidth)
        {
            RoadWidth = transitGroupedByWidth.Key;
            RunningLength = transitGroupedByWidth.Sum(r => r.Centerline.Length());
            Area = RunningLength * RoadWidth;
        }
    }
}
