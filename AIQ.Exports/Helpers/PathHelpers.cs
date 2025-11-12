using System.Reflection;

namespace AIQ.Exports.Helpers;
public static class PathHelpers
{
    public static string GetLogoTemplatePath()
    {
        return Path.Combine(GetExportResourcesFolder(), "LEGO.png");
    }

    public static string GetPowerpointTemplatePath()
    {
        return Path.Combine(GetExportResourcesFolder(), "AIQUrbanTemplate.pptx");
    }

    public static string GetExportResourcesFolder()
    {
        var directory = Directory.GetParent(Assembly.GetExecutingAssembly().Location)!;
        var path = Path.Combine(directory.FullName, "AIQ Export Resources");
        return path;
    }
}
