namespace ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
internal class SelectedObjectMetricsMessage:IMessageToUI
{
    public string Type { get; } = nameof(SelectedObjectMetricsMessage);
    public string SelectedObjectType { get; } = "";
    public List<(string Title,string Data)> Metrics { get; } = new();

    public SelectedObjectMetricsMessage(
        string selectedObjectType,
        List<(string Title, string Data)> metrics)
    {
        SelectedObjectType = selectedObjectType;
        Metrics = metrics;
    }
}