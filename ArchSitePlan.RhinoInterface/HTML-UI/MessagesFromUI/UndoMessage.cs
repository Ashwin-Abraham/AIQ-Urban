using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI
{
    public class UndoMessage:IMessageFromUI<StudyAreaMetricsMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Undo;

        public StudyAreaMetricsMessage HandleMessage()
        {
            ModelBuilderPipelines.TryRollBack();
            return new();
        }
    }
}
