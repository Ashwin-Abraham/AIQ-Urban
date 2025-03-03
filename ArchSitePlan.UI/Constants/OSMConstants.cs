using System.IO;
using System.Reflection;

namespace ArchSitePlan.UI.Constants;

public static class OSMConstants
{
    public const string OsmIdAttributeKey = "OsmId";
    public static Dictionary<long, string> OsmIdsByFile = new();
    public static readonly string OsmFileDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
}
