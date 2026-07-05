using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public class ToolSelectionMessage : IMessageFromUI<EmptyMessage>
{
    public TypeOfMessage MessageType => TypeOfMessage.ToolSelection;

    public EmptyMessage HandleMessage()
    {
        return new EmptyMessage();
    }
}
