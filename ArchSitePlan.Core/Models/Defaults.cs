using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.Models
{
    public static class Defaults
    {
        public static Dictionary<Landuse, Color> LandUseColours = new Dictionary<Landuse, Color>()
        {
            { Landuse.Residential, Color.Yellow },
            { Landuse.Sport, Color.DarkGreen },
            { Landuse.Retail, Color.DarkMagenta },
            { Landuse.Commercial, Color.MediumPurple },
            { Landuse.Civic, Color.Goldenrod },
            { Landuse.EntertainmentAndCulture, Color.Lavender },
            { Landuse.FoodAndBeverage, Color.Beige },
            { Landuse.Transit, Color.DarkOrange },
            { Landuse.Hospitality, Color.Chocolate },
            { Landuse.Religious, Color.RebeccaPurple },
            { Landuse.Undefined,  Color.DarkSlateGray }
        };
    }
}
