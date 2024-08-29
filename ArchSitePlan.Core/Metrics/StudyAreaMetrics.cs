using ArchSitePlan.Core.Models;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Metrics
{
    public class StudyAreaMetrics
    {
        public PolyCurve Boundary { get; private set; }
        public double BoundedArea { get; private set; }
        public List<ParcelMetrics> ParcelMetrics = new();
        //public RoadMetrics RoadMetric = new();
        private void UpdateParcelMetrics(List<Parcel> parcels)
        {
            ParcelMetrics = new();

            foreach(Parcel p in parcels)
            {
                ParcelMetrics.Add(new ParcelMetrics(p));
            }
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
