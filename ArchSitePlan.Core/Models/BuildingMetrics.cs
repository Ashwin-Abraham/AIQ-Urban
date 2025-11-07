using BH.Engine.Geometry;

namespace ArchSitePlan.Core.Models;
public class BuildingMetrics
{
    public double BUA { get; init; }
    public double GroundCoverage { get; init; }
    public List<(Landuse? Use, double BUA)> BuaPerUse { get; init; }

    public BuildingMetrics(Building building)
    {
        BuaPerUse = CalculateBuaPerUse(building);
        BUA = BuaPerUse.Sum(x => x.BUA);
        GroundCoverage = building.FloorPlates
            .Where(fp => Math.Abs(fp.Profile.IArea()) > 1E-06)
            .MinBy(fp => fp.Profile.ExternalBoundary.IStartPoint().Z)?
            .Profile.IArea() ?? 0;
    }

    private static List<(Landuse? Use, double BUA)> CalculateBuaPerUse(Building building)
    {
        var groupedByUse = building.FloorPlates.GroupBy(fp => fp.Use);
        var buaPerUse = new List<(Landuse?, double)>();
        
        foreach (var useGroup in groupedByUse)
        {
            double area = useGroup.Sum(x => x.AreaAbs());
            buaPerUse.Add((useGroup.Key, area));
        }

        return buaPerUse;
    }
}