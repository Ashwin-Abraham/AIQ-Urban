using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;
public enum TypeOfMessage
{
    Transit,
    Landuse,
    Building,
    Divider,
    Zone,
    ToolSelection,
    LanduseListEdit,
    ReadOsm,
    PickFile
}

// out T is the resulting output message created after handling the input
public interface IMessageFromUI<out T> where T : IMessageToUI
{
    public TypeOfMessage MessageType{ get; init; }
    public T HandleMessage();
}
