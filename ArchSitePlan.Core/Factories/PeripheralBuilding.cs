using ArchSitePlan.Core.Models;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories;

public class PeripheralBuilding : Building
{
    public PeripheralBuilding(
    Mesh envelope,
    List<PlanarSurface> floorPlates,
    double floorToFloorHeight)
    : base(envelope, floorPlates, floorToFloorHeight)
    {
    }
}
