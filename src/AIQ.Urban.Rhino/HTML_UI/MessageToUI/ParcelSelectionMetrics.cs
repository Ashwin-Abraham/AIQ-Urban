namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

public record ParcelSelectionMetrics(
    int Count,
    double LandArea,
    double Perimeter,
    double BuiltUpArea,
    double BuiltFootprint,
    double FAR,
    double CoveragePercentage)
{
    public static ParcelSelectionMetrics Empty { get; } = new(
        Count: 0,
        LandArea: 0,
        Perimeter: 0,
        BuiltUpArea: 0,
        BuiltFootprint: 0,
        FAR: 0,
        CoveragePercentage: 0);
}
