using ArchSitePlan.Core.Models;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Metrics
{
    public class StudyAreaMetrics
    {
        public double BoundedArea { get; private set; } = 25;
        public double FAR { get; private set; } = 12;
        public double DevelopableArea { get; private set; } = 5;
        public double BuiltArea { get; private set; } = 2;
        public List<ParcelMetrics> ParcelMetrics = new();
        //public RoadMetrics RoadMetric = new();
        private void UpdateParcelMetrics(List<Parcel> parcels)
        {
            ParcelMetrics = new();

            foreach(Parcel p in parcels)
            {
                ParcelMetrics.Add(new ParcelMetrics(p));
            }

            BoundedArea = ParcelMetrics.Sum(p => p.Area);
        }

        public StudyAreaMetrics(List<Parcel> parcels)
            //List<Building> buildings, 
            //List<Road> roads,
            //List<PlanarSurface> roadRoWs) 
        {
            UpdateParcelMetrics(parcels);
        }
    }
}
