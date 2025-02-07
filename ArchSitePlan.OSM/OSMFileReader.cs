using ArchSitePlan.OSM.Extensions;
using ArchSitePlan.OSM.Models;
using OsmSharp;
using OsmSharp.Streams;

namespace ArchSitePlan.OSM;

public class OSMFileReader
{
    public static bool TryReadFile(string filePath, out OsmModel? model)
    {
        using (var fileStream = File.OpenRead(filePath))
        {
            // Create an OSM stream source from the XML file.
            var source = new XmlOsmStreamSource(fileStream);
            var ways = source.Where(x => x is Way && x.HasLanduse()).ToList();

            var uses = ways.Select(x => x.Tags[Keys.Landuse]).ToList();
        }
    }
}
