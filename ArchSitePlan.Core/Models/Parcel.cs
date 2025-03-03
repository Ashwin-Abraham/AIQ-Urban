using ArchSitePlan.Core.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models
{
    public class Parcel
    {
        public PlanarSurface Boundary {  get; set; }
        public Landuse LandUse { get; set; }
        public Guid Id { get; set; }
        internal Parcel(PlanarSurface boundary, Landuse use, Guid? id = null)
        {
            Boundary = boundary;
            LandUse = use;
            Id = id ?? Guid.NewGuid();
        }
        internal Parcel(Parcel parcel, PlanarSurface boundary):this(boundary, parcel.LandUse)
        {
            Id = parcel.Id;
        }

        public double Area()
        {
            return Math.Abs(Boundary.Area());
        }

        public double Coverage()
        {
            var buildings = GetParcelsBuildings();
            double footPrint = buildings.Sum(b => b.FootprintArea());
            double coveragePctg = footPrint / Area() * 100;

            return coveragePctg;
        }

        private List<Building> GetParcelsBuildings()
        {
            var buildings = CoreModel.Buildings
                .Where(b => Boundary.ExternalBoundary.IIsContaining(b.Origin.ObjectToList()))
                .ToList();

            return buildings;
        }

        public double Perimeter()
        {
            double perimeter = Boundary.ExternalBoundary.ILength() 
                + Boundary.InternalBoundaries.Sum(crv => crv.ILength());
            return perimeter;
        }

        public double BUA()
        {
            var bldgs = GetParcelsBuildings();
            double bua = bldgs.Sum(b => b.BUA());

            return bua;
        }
    }
}
