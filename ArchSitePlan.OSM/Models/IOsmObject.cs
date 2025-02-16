using System.Drawing;

namespace ArchSitePlan.OSM.Models;

public interface IOsmObject 
{ 
    public long? Id { get; init; }
    public Color Color { get; init; }
}