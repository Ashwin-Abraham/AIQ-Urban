using System.Drawing;
using ArchSitePlan.Core.Extensions;

namespace ArchSitePlan.Core.Models;

public class Landuse(string name, string colorHex)
{
    public string Name { get; set; } = name;
    public Color Color { get; set; } = ColorTranslator.FromHtml(colorHex);

    public Color GetBuildingColor(int transparency = 150, double darkenPctg  =0.3)
    {
        return this.Color.SetTransparency(transparency).Darken(darkenPctg);
    }
}
