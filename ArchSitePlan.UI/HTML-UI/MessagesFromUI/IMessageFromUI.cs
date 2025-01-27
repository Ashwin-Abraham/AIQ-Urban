using ArchSitePlan.UI.HTML_UI.MessageToUI;

namespace ArchSitePlan.UI.HTML_UI.MessagesFromUI;
public enum TypeOfMessage
{
    Transit,
    Landuse,
    Building,
    Divider,
    Zone,
    ToolSelection
}
public interface IMessageFromUI<out T> where T : IMessageToUI
{
    public TypeOfMessage MessageType{ get; init; }
    public T HandleMessage();
}
