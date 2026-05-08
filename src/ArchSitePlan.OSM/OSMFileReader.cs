using ArchSitePlan.OSM.Extensions;
using ArchSitePlan.OSM.Models;
using OsmSharp;
using OsmSharp.Streams;

namespace ArchSitePlan.OSM;

public class OSMFileReader
{
    public static List<(string Tag, string Value)>? FindObjectTags(string? filePath, long osmId)
    {
        if (filePath!=null && !File.Exists(filePath)) return null;

        using (var fileStream = File.OpenRead(filePath))
        {
            // Create an OSM stream source from the XML file.
            var source = new XmlOsmStreamSource(fileStream);
            var osmObj = source.FirstOrDefault(x => x.Id.HasValue && x.Id == osmId);

            return osmObj?.Tags?.Select(x => (x.Key, x.Value)).ToList();
        }
    }

    public static bool TryMakeOSMModelFromFile(string filePath, out OsmModel? model)
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
