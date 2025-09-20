using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI
{
    public class UndoMessage:IMessageFromUI<ModelStateMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Undo;

        public ModelStateMessage HandleMessage()
        {
            ModelBuilderPipelines.TryRollBack();
            return new();
        }
    }
}
