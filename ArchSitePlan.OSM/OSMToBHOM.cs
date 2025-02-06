using ArchSitePlan.OSM.Extensions;
using OsmSharp;
using OsmSharp.Streams;

namespace ArchSitePlan.OSM
{
    public static class OSMToBHOM
    {
        public static void ReadFile(string filePath)
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
}
