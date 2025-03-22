using ArchSitePlan.Core.Metrics;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models
{
    public class StudyArea
    {
        public ICurve? Boundary { get; private set; }
        public Guid Id { get; set; }
        public string Name { get; private set; }
        public double Area { get; private set; }

        public StudyAreaMetrics Metrics { get; private set; }
        public StudyArea(
            ICurve? boundary, 
            Guid id, 
            string name,
            List<Parcel> parcels,
            List<Building> buildings,
            List<Road> transitRoutes)
        {
            Boundary = boundary;
            Id = id;
            Name = name;
            Area = Math.Abs(boundary?.IArea()?? 0);
            Metrics = new StudyAreaMetrics(
                boundary,
                parcels,
                buildings,
                transitRoutes);
        }

        public StudyArea(
            StudyArea study,
            List<Parcel> parcels,
            List<Building> buildings,
            List<Road> transitRoutes) 
            : this(study.Boundary, study.Id, study.Name, parcels, buildings, transitRoutes)
        { }
    }
}
