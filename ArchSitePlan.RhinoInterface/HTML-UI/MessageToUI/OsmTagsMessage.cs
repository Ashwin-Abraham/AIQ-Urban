namespace ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;

public class OsmTagsMessage(List<(string,string)> tags) : IMessageToUI
{
    public string Type => nameof(OsmTagsMessage);

    public List<(string Key, string Value)> Tags { get; set; } = tags;
}
