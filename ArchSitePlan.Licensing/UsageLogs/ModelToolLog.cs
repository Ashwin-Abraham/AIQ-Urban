namespace ArchSitePlan.RhinoInterface.UsageLogs;

public class ModelToolLog:IFeatureUsageLog
{
    public int Transit { get; set; } = 0;
    public int Landuse { get; set; } = 0;
    public int Building { get; set; } = 0;
    public int Divider { get; set; } = 0;
    public int Zoning { get; set; } = 0;

    public int Sum => Transit + Landuse + Building + Divider + Zoning;

    public void Add(ModelToolLog log)
    {
        Transit += log.Transit;
        Landuse += log.Landuse;
        Building += log.Building;
        Divider += log.Divider;
        Zoning += log.Zoning;
    }
}
