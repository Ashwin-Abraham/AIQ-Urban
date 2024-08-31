using BH.oM.Geometry;
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
        public List<PlanarSurface> CombinedRoWs { get; set; }
    }
}
