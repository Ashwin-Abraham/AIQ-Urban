
using ArchSitePlan.RhinoInterface.Actions;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Newtonsoft.Json;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;

public class ToolSelectionMessage : IMessageFromUI<EmptyMessage>
{
    public TypeOfMessage MessageType { get; init; } = TypeOfMessage.ToolSelection;
    
    [JsonProperty("tool")]
    public ModellingTool Tool { get; init; }
    
    public EmptyMessage HandleMessage()
    {
        RhinoEventHandler.HTMLViewModel.SetModellingMode(Tool);
        return new();
    }
}


