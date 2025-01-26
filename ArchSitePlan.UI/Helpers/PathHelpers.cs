using System.IO;
using System.Reflection;

namespace ArchSitePlan.UI.Helpers;

public static class PathHelpers
{
    public static string GetResourcePath(string fileName)
    {
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var resourceLocation = Path.GetDirectoryName(assemblyPath);
        return Path.Combine(resourceLocation, "EmbeddedResources", fileName);
    }
}
