using ArchSitePlan.Core.Models;

namespace ArchSitePlan.Core.Metrics
{
    public class ProjectMetrics
    {
        public List<ParcelMetrics> Parcels = new();
        public void UpdateParcelMetrics(List<Parcel> parcels)
        {
            Parcels = new();

            foreach(Parcel p in parcels)
            {
                Parcels.Add(new ParcelMetrics(p));
            }
        }
    }
}
