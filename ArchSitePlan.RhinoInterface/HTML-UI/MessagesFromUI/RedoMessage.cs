using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI
{
    public class RedoMessage:IMessageFromUI<StudyAreaMetricsMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Redo;

        public StudyAreaMetricsMessage HandleMessage()
        {
            ModelBuilderPipelines.TryRollForward();
            return new();
        }
    }
}
