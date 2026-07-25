namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

public record TransitSelectionMetrics(
    int Count,
    double RunningLength,
    double Area)
{
    public static TransitSelectionMetrics Empty { get; } = new(
        Count: 0,
        RunningLength: 0,
        Area: 0);
}
