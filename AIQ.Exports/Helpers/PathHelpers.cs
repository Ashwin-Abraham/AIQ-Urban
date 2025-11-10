using System.Reflection;

namespace AIQ.Exports.Helpers;
public static class PathHelpers
{
    public static string GetPowerpointTemplatePath()
    {
        var directory = Directory.GetParent(Assembly.GetExecutingAssembly().Location)!;
        var path = Path.Combine(directory.FullName, "AIQ Export Resources//AIQUrbanTemplate.pptx");
        return path;
    }
}
