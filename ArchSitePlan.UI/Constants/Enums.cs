using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Constants
{
    public static class Enums
    {
        public enum ModellingTool
        {
            Transit,
            Dividers,
            LandUse,
            Building,
            Zones,
            Export,
            Parking,
            None
        }

        public enum BuildingSelectorMode
        {
            Footprint,
            Mass,
            None
        }
    }
}
