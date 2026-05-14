using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.Models.Metrics;
public class BuildingMetrics : IMetrics
{
    public double BuiltUpArea { get; }
    public double Volume { get; }
    public double FootprintArea { get; }
    public double Height { get; }

    public Dictionary<Use, double> AreaPerUse { get; }

    public BuildingMetrics(Building building)
    {
        var groundFloor = building.GroundFloor();
        FootprintArea = groundFloor.Outline.Area();
        Volume = building.Envelope.Volume();
        Height = building.Height();
        AreaPerUse = ConstructAreaPerUse(building.FloorPlates);
        BuiltUpArea = AreaPerUse.Sum(x => x.Value);
    }

    private static Dictionary<Use, double> ConstructAreaPerUse(IReadOnlyList<FloorPlate> floorPlates)
    {
        if (floorPlates.Count == 0)
            return [];

        var byUse = floorPlates
            .GroupBy(x => x.Use)
            .ToDictionary(grp => grp.Key, grp => grp.Sum(item => item.Area()));

        return byUse;
    }
}
