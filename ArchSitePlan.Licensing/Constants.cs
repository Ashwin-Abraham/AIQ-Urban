using System.Reflection;

namespace ArchSitePlan.Licensing;

public static class Constants
{
    public const string LastActivatedMetadata = "Last Activation";
    public const string MachineCode = "Machine Code";
    public const string UsageLog = "Usage Log";

    // Get the directory of the executing assembly
    public static string AssemblyDirectory { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    public static string LicensePath { get; } = Path.Combine(AssemblyDirectory, "license.json");
}
