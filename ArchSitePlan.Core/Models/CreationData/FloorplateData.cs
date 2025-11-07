using ArchSitePlan.Core.Models;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models.CreationData;
public class FloorplateData
{
    public PlanarSurface Geometry { get; }
    public Guid Id { get; }
    public LanduseData? Use { get; set; }

    public FloorplateData(PlanarSurface geometry, Guid id, LanduseData? use)
    {
        Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        Id = id;
        Use = use;
    }
}
