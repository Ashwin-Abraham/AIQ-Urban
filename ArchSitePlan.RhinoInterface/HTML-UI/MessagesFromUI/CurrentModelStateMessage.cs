using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Rhino;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;
public class CurrentModelStateMessage : IMessageFromUI<ModelStateMessage>
{
    public TypeOfMessage MessageType { get; init; } = TypeOfMessage.ModelState;

    public ModelStateMessage HandleMessage()
    {
        ModelBuilderPipelines.ReloadAppPipeline(RhinoDoc.ActiveDoc, ModellingTool.Transit);
        return new();
    }
}

