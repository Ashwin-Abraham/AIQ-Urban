
using ArchSitePlan.UI.UsageLogs;

namespace ArchSitePlan.RhinoInterface.UsageLogs;

public class UsageLog:IFeatureUsageLog
{
    public ImportToolLog ImportLog { get; set; } = new();
    public ExportToolLog ExportLog { get; set; } = new();
    public ModelToolLog ModelLog { get; set; } = new();
    public int Sum => ImportLog.Sum + ExportLog.Sum + ModelLog.Sum;

    public void Add(UsageLog log)
    {
        ImportLog.Add(log.ImportLog);
        ExportLog.Add(log.ExportLog);
        ModelLog.Add(log.ModelLog);
    }
}