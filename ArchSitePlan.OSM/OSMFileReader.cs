using ArchSitePlan.OSM.Extensions;
using ArchSitePlan.OSM.Models;
using OsmSharp;
using OsmSharp.Streams;

namespace ArchSitePlan.OSM;

public class OSMFileReader
{
    public static bool TryReadFile(string filePath, out OsmModel? model)
    {
        model = null;
        if (!File.Exists(filePath)) return false;

        using (var fileStream = File.OpenRead(filePath))
        {
            // Create an OSM stream source from the XML file.
            var source = new XmlOsmStreamSource(fileStream);
            model = OsmModel.FromOsmSource(source);

            return model != null;
        }
    }
}
