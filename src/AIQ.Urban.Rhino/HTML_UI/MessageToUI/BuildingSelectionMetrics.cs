namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

public record BuildingSelectionMetrics(
    int Count,
    double BuiltUpArea,
    double FootprintArea,
    double Volume)
{
    public static BuildingSelectionMetrics Empty { get; } = new(
        Count: 0,
        BuiltUpArea: 0,
        FootprintArea: 0,
        Volume: 0);
}
