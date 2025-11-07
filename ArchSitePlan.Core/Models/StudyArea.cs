using ArchSitePlan.Core.Metrics;
using BH.oM.Geometry;
using Newtonsoft.Json;

namespace ArchSitePlan.Core.Models
{
    public class StudyArea
    {
        public ICurve? Boundary { get; private set; }
        public Guid Id { get; set; }
        public string Name { get; private set; }
        public double Area { get; private set; }

        public List<Parcel> Parcels { get; set; }
        public List<Road> Transit { get;set; }
        public List<Building> Buildings { get; set; }


        [JsonConstructor]
        public StudyArea(
            ICurve? boundary, 
            Guid id, 
            string name,
            List<Parcel> parcels,
            List<Building> buildings,
            List<Road> transit)
        {
            Boundary = boundary;
            Id = id;
            Name = name;
            Area = parcels.Sum(x => x.Area());
            Parcels = parcels;
            Buildings = buildings;
            Transit = transit;
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
