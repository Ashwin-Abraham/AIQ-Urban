using AIQ.Primitives.Extensions;
using Newtonsoft.Json;
using System.Drawing;

namespace ArchSitePlan.Core.Models;

public class Landuse
{
    public string Name { get; set; }

    public Color Color { get; set; }
    
    public Guid Id { get;set;} 

    public Landuse(string name, Guid id, string colorHex, int alpha = 255)
    {
        Name = name;
        Color = ColorTranslator.FromHtml(colorHex).SetTransparency(alpha); 
        Id = id;
    }

    [JsonConstructor]
    public Landuse(string name, Color color, Guid id)
    {
        Name = name;
        Color = color;
        Id = id;
    }

    public (string, string) ToStringTuple()
    {
        var color = $"{Color.R},{Color.G},{Color.B}";
        return (Name, color);
    }

    public Color GetBuildingColor(int transparency = 150, double darkenPctg  = 0.3)
    {
        return this.Color.SetTransparency(transparency).Darken(darkenPctg);
    }
}
