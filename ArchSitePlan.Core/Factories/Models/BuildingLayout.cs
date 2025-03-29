using ArchSitePlan.Core.Models;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories.Models;

public class BuildingLayout
{
    public ICurve SiteBoundary { get; }
    public ICurve SetBackBoundary { get; }
    public List<Building> Buildings { get; }

    public BuildingLayout(
        ICurve siteBoundary,
        ICurve setBackBoundary,
        List<Building> buildings)
    {
        SiteBoundary = siteBoundary;
        SetBackBoundary = setBackBoundary;
        Buildings = buildings;
    }
}

