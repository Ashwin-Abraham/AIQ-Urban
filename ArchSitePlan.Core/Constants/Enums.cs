namespace ArchSitePlan.Core.Constants;

public static class Enums
{
    public enum TransitType
    {
        Primary,
        Secondary,
        Tertiary,
        Footpath,
        Tram,
        Rail,
        Cycleway
    }
    public enum EditType
    {
        Add,
        Remove,
        Edit,
        Divide,
        ClearAll,
        UpdateUse
    }
}
