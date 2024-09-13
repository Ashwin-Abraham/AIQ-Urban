using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Constants
{
    public static class Enums
    {
        public enum Landuse
        {
            Undefined,
            Residential,
            Commercial,
            Retail,
            EntertainmentAndCulture,
            FoodAndBeverage,
            Sport,
            Hospitality,
            Religious,
            Civic,
            Transit
        }

        public enum TransitType
        {
            Primary,
            Secondary,
            Tertiary,
            Footpath,
            Tram,
            Rail,
            Cycleway
        }
    }
}
