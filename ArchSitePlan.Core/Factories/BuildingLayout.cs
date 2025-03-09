using ArchSitePlan.Core.Models;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories;

public class BuildingLayout
{
    public PolyCurve SiteBoundary { get; }
    public PolyCurve SetBackBoundary { get; }
    public List<Building> Buildings { get; }

    public BuildingLayout(
        PolyCurve siteBoundary,
        PolyCurve setBackBoundary,
        List<Building> buildings)
    {
        SiteBoundary = siteBoundary;
        SetBackBoundary = setBackBoundary;
        Buildings = buildings;
    }
}

