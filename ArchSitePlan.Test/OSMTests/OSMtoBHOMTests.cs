using ArchSitePlan.DebugModel;
using ArchSitePlan.OSM;

namespace ArchSitePlan.Test.OSMTests;

public class OSMtoBHOMTests
{
    [Fact]
    public static void ReadOSM()
    {
        string filePath = @"C:\Users\AshwinAbraham\Downloads\map.osm";

        var result = OSMFileReader.TryMakeOSMModelFromFile(filePath, out var model);

        // Preview
#if DEBUG
        var roadCLs = model.Highways.Select(x => x.Centerline).ToList();
        var landuse = model.LandParcels.Select(x => x.Boundary).ToList();
        roadCLs.ToElements().Visualize();
        landuse.ToElements().Visualize();
#endif

        // Assert
        Assert.True(result);
        Assert.True(model.Highways.Count > 0);
        Assert.True(model.LandParcels.Count > 0);
    }
}
