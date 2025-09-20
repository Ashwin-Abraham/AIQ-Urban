using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI
{
    public class RedoMessage:IMessageFromUI<ModelStateMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Redo;

        public ModelStateMessage HandleMessage()
        {
            ModelBuilderPipelines.TryRollForward();
            return new();
        }
    }
}
