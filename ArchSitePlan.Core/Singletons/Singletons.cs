using ArchSitePlan.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Singletons
{
    public static class Singletons
    {
        public static RoadNetwork RoadNetwork { get; set; } = new RoadNetwork();
    }
}
