using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models;
public class FloorPlate
    (PlanarSurface _profile,
    Guid _id,
    Landuse? _use
    )
{
    public PlanarSurface Profile { get; set; } = _profile;
    public Guid Id { get; private set; } = _id;
    public Landuse? Use { get; set; } = _use;

    public void UpdateId(Guid newId)
    {
        Id = newId;
    }

    internal double AreaAbs()
    {
        return Math.Abs(Profile.Area());
    }
}
