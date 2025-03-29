
using ArchSitePlan.RhinoInterface.Actions;
using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Newtonsoft.Json;
using Rhino;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;

public class ToolSelectionMessage : IMessageFromUI<EmptyMessage>
{
    public TypeOfMessage MessageType { get; init; } = TypeOfMessage.ToolSelection;
    
    [JsonProperty("tool")]
    public ModellingTool Tool { get; init; }
    
    public EmptyMessage HandleMessage()
    {
        if (Tool == ModellingTool.None)
            LayerManager.ToggleLockModelLayers(RhinoDoc.ActiveDoc, true);

        RhinoEventHandler.HTMLViewModel.AppMode = Tool;
        LayerManager.ToggleLayerStateBasedOnAppMode(RhinoDoc.ActiveDoc, Tool);
        RhinoDoc.ActiveDoc.Views.Redraw();

        return new();
    }
}


