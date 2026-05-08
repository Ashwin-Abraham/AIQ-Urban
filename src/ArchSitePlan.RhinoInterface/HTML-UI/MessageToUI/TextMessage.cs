namespace ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;

public class TextMessage(string text) : IMessageToUI
{
    public string Type => nameof(TextMessage);
    public string Text { get; set; } = text;
}
