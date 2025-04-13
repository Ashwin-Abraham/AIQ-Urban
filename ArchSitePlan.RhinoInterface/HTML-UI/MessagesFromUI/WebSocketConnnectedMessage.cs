using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;
public class WebSocketConnnectedMessage:IMessageFromUI<AppStateMessage>
{
    public TypeOfMessage MessageType { get; init; } = TypeOfMessage.WebSocketConnected;

    public AppStateMessage HandleMessage()
    {
        return new AppStateMessage();
    }
}
