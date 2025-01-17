
namespace ArchSitePlan.UI.UsageLogs;

public class ImportToolLog:IFeatureUsageLog
{
    public int OSM { get; set; } = 0;
    public int Sum => OSM;

    public void Add(ImportToolLog log)
    {
        OSM += log.OSM;
    }
}
