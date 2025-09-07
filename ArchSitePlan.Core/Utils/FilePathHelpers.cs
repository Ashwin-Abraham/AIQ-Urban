namespace ArchSitePlan.Core.Utils;
public static class FilePathHelpers
{
    public static string GetExecutingAssemblyDirectory()
    {
        var executingAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var executingDirectory = Path.GetDirectoryName(executingAssemblyPath);

        return executingDirectory!;
    }
}
