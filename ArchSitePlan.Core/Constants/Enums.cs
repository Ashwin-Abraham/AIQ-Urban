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
            Resi_Low_Density,
            Resi_High_Density,
            Parking,
            Transit,
            Retail,
            Office,
            Food_And_Beverage,
            Religious,
            Medical,
            Mixed_Use,
            Civic,
            Sport,
            Parks_And_Open_Space,
            Forest,
            Water
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
        public enum EditType
        {
            Add,
            Remove,
            Edit,
            Divide,
            ClearAll
        }
    }
}
