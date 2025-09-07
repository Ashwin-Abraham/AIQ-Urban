using ArchSitePlan.Core.Utils;

namespace ArchSitePlan.Core.Constants;
public class FilePathContants
{
    public static string AppStateLogPath =Path.Combine(
        FilePathHelpers.GetExecutingAssemblyDirectory(),
        "ArchSitePlanAppStateLog.json");
}
