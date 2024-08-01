using BH.oM.Geometry;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Models
{
    public class RoadNetwork
    {
        public List<Road> Roads { get; set; }

        public List<PlanarSurface> ROWs { get; set; }
        public RoadNetwork() { }

        private void CalculateROWs()
        {
        }

        public void AddRoad(ICurve centerLine, double width)
        {
            Roads.Add(new Road(centerLine, width));
        }
    }
}
