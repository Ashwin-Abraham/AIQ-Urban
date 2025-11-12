using BH.Engine.Geometry;

namespace ArchSitePlan.Core.Models;
public class BuildingMetrics
{
    public Guid EnvelopeId { get; init; }
    public double BUA { get; init; }
    public double GroundCoverageArea { get; init; }
    public List<(Landuse? Use, double BUA)> BuaPerUse { get; init; }

    public BuildingMetrics(Building building)
    {
        BuaPerUse = CalculateBuaPerUse(building);
        BUA = BuaPerUse.Sum(x => x.BUA);
        GroundCoverageArea = building.FloorPlates
            .Where(fp => Math.Abs(fp.Profile.IArea()) > 1E-06)
            .MinBy(fp => fp.Profile.ExternalBoundary.IStartPoint().Z)?
            .Profile.IArea() ?? 0;
        EnvelopeId = building.EnvelopeId;
    }

    public BuildingMetrics(
        Guid envelopeId,
        double groundCoverage,
        List<(Landuse? Use, double BUA)> buaPerUse)
    {
        EnvelopeId = envelopeId;
        BUA = buaPerUse.Sum(x => x.BUA);
        GroundCoverageArea = groundCoverage;
        BuaPerUse = buaPerUse;
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