using ArchSitePlan.Core.Models;
using System.Drawing;
using System.Runtime.Intrinsics.X86;

namespace ArchSitePlan.Core.Pipeline.CreationData;
public class LanduseData
{
    public string Name;
    public Color Color;
    public Guid Id;

    public LanduseData(string name, Color color, Guid id)
    {
        Name = name;
        Color = color;
        Id = id;
    }
    public LanduseData(string name, int argbColor, Guid id)
        : this(
              name, 
              Color.FromArgb(argbColor), 
              id)
    { }

    public Landuse ToLanduse()
    {
        return new Landuse(Name, Color, Id);
    }
}
