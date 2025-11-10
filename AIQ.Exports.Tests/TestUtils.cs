using System.Reflection;

namespace AIQ.Exports.Tests;
public static class TestUtils
{
    public static string GetTestResourcePath(string file)
    {
        var folder = Directory.GetParent(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(
            folder.FullName,
            "ExportTestResources",
            file);
    }

    public static string GetPptTargetPathInDownloads()
    {
        return Path.Combine(
            GetDownloadsFolder(),
            "ProjectReport_AIQ_Tests.pptx");
    }

    public static string GetDownloadsFolder()
    {
        string? downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        return downloadsPath;
    }
}
