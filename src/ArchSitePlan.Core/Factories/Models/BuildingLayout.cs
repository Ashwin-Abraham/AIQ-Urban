using ArchSitePlan.Core.Models;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories.Models;

public class BuildingLayout
{
    public Guid HostParcelCoreModelId { get; set; }
    public ICurve SiteBoundary { get; }
    public ICurve SetBackBoundary { get; }
    public List<Building> Buildings { get; }

    public BuildingLayout(
        ICurve siteBoundary,
        ICurve setBackBoundary,
        List<Building> buildings,
        Guid? hostParcelCoreModelId)
    {
        SiteBoundary = siteBoundary;
        SetBackBoundary = setBackBoundary;
        Buildings = buildings;
        HostParcelCoreModelId = hostParcelCoreModelId?? Guid.NewGuid();
    }
}

