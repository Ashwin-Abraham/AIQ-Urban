using System.Drawing;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.Models
{
    public static class Defaults
    {
        public static Dictionary<Landuse, Color> LandUseColours = new Dictionary<Landuse, Color>()
        {
            { Landuse.Undefined, Color.Crimson },
            { Landuse.Resi_Low_Density, ColorTranslator.FromHtml("#FFFBC3")},
            { Landuse.Resi_High_Density, ColorTranslator.FromHtml("#EEE023")},
            { Landuse.Parking, ColorTranslator.FromHtml("#636363")},
            { Landuse.Transit, ColorTranslator.FromHtml("#B6B6B6")},
            { Landuse.Retail, ColorTranslator.FromHtml("#8F3186")},
            { Landuse.Office, ColorTranslator.FromHtml("#CFB9A1")},
            { Landuse.Food_And_Beverage, ColorTranslator.FromHtml("#3F8F74")},
            { Landuse.Religious, ColorTranslator.FromHtml("#97662C")},
            { Landuse.Medical, ColorTranslator.FromHtml("#F8696B")},
            { Landuse.Mixed_Use, ColorTranslator.FromHtml("#ABEDE6")},
            { Landuse.Civic, ColorTranslator.FromHtml("#F89C1B")},
            { Landuse.Sport, ColorTranslator.FromHtml("#B1DE37")},
            { Landuse.Parks_And_Open_Space, ColorTranslator.FromHtml("#D3FCD4")},
            { Landuse.Forest, ColorTranslator.FromHtml("#1D7320")},
            { Landuse.Water, ColorTranslator.FromHtml("#3B83BA")},
        };
    }
}
