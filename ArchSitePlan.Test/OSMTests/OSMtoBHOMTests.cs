using ArchSitePlan.OSM;

namespace ArchSitePlan.Test.OSMTests;

public class OSMtoBHOMTests
{
    [Fact]
    public static void ReadOSM()
    {
        string filePath = @"C:\Users\AshwinAbraham\Downloads\map.osm";
        OSMToBHOM.ReadFile(filePath); 
    }
}
