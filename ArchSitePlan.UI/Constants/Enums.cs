using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Constants
{
    public static class Enums
    {
        public enum AppMode
        {
            Road,
            Separator,
            LandUse,
            Building,
            StudyArea,
            Export,
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
