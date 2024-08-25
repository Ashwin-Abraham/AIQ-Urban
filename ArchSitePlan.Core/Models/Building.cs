using BH.Engine.Geometry;
using BH.oM.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Models
{
    public class Building
    {
        public PlanarSurface BaseOutline { get; set; }
        public int FloorCount { get; set; }
        public double FloorToFloorHeight { get; set;}
        public Guid Id { get; init;}
        public Guid OutputMassId { get; set;} = Guid.Empty;

        public List<(PlanarSurface Profile, Guid Id)> FloorPlates { get; set; }

        public Building(
            PlanarSurface baseOutline, 
            int floorCount, 
            double floorToFloorHeight,
            Guid id)
        {
            BaseOutline = baseOutline;
            FloorCount = floorCount;
            FloorToFloorHeight = floorToFloorHeight;
            FloorPlates = CreateFloorPlates();
            Id = id;
        }

        private List<(PlanarSurface, Guid)> CreateFloorPlates()
        {
            var floorPlates = new List<(PlanarSurface,Guid)>();

            for(int i = 0; i < FloorCount; i++)
            {
                var mover = Create.Vector(0,0, i * FloorToFloorHeight);
                var floor = BaseOutline.Translate(mover);
                floorPlates.Add((floor, Guid.Empty));
            }
            return floorPlates;
        }

        public Extrusion GetExtruded3D()
        {
            Extrusion extrudedMass = Create.Extrusion(
                BaseOutline.ExternalBoundary, 
                Create.Vector(0, 0, FloorToFloorHeight * FloorCount),
                true);

            return extrudedMass;
        }
        public void UpdateProperties(PlanarSurface footPrint, int floorCount, double floorToFloorHeight)
        {
            BaseOutline = footPrint;
            FloorCount = floorCount;
            FloorToFloorHeight = floorToFloorHeight;
            FloorPlates = CreateFloorPlates();
        }
    }
}
