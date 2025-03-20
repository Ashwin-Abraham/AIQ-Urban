
namespace ArchSitePlan.RhinoInterface.UsageLogs;

public class ExportToolLog: IFeatureUsageLog
{
    public int Excel { get; set; } = 0;

    public int Sum => Excel;

    public void Add(ExportToolLog log)
    {
        Excel += log.Excel;
    }
}
