namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public enum TypeOfMessage
{
    Transit,
    Building,
    Zone,
    Landuse,
    ChangeBuildingUse,
    ToolSelection
}

public interface IMessageFromUI<out T> where T : MessageToUI.IMessageToUI
{
    TypeOfMessage MessageType { get; }
    T HandleMessage();
}
