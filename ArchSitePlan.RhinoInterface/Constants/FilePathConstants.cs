using System.Reflection;

namespace ArchSitePlan.RhinoInterface.Constants;
public static class FilePathConstants
{
#if DEBUG || DEBUG_NOVIZ
    public static string VueHtmlPath { get; } = @"C:\Users\Ashwin\source\repos\aiqurban-ui\dist";
#endif

#if RELEASE
    public static string VueHtmlPath { get; } = GetReleaseVueHtmlPath();
#endif

    private static string GetReleaseVueHtmlPath()
    {
        var directory = ExecutingAssemblyDirectory();
        var vueHtmlPath = Path.Combine(directory.FullName, "dist");
        return vueHtmlPath;
    }

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
