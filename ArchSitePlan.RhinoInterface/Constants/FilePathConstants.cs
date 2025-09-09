
using System.Reflection;

namespace ArchSitePlan.RhinoInterface.Constants;
public static class FilePathConstants
{
    public const string VueHtmlPath = @"C:\Users\Ashwin\source\repos\aiqurban-ui\dist";

    public static string GetUsageLogPath()
    {
        var directory = ExecutingAssemblyDirectory();
        var usageLogPath = Path.Combine(directory.FullName, "UsageLog.txt");

        return usageLogPath;
    }

    public static DirectoryInfo ExecutingAssemblyDirectory()
    {
        return Directory.GetParent(Assembly.GetExecutingAssembly().Location)!;
    }
}
