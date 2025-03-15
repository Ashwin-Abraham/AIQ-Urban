using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories.Models;

public class BuildingCreationSpecs
{
    public PolyCurve FootPrint { get; set; }
    public double FootprintArea { get; set; }
    public int FloorCount { get; set; }

    public BuildingCreationSpecs(PolyCurve footPrint, double footprintArea, int floorCount)
    {
        FootPrint = footPrint;
        FootprintArea = footprintArea;
        FloorCount = floorCount;
    }
}

