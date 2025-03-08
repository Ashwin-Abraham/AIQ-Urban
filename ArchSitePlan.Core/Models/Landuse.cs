using System.Drawing;
using AIQ.Primitives.Extensions;

namespace ArchSitePlan.Core.Models;

public class Landuse(string name, string colorHex, int alpha = 255)
{
    public string Name { get; set; } = name;
    public Color Color { get; set; } = ColorTranslator.FromHtml(colorHex).SetTransparency(alpha);

    public Color GetBuildingColor(int transparency = 150, double darkenPctg  =0.3)
    {
        return this.Color.SetTransparency(transparency).Darken(darkenPctg);
    }
}
