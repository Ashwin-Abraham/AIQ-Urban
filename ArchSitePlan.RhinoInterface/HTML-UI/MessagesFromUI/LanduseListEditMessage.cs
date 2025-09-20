
using ArchSitePlan.Core.ModelEditActions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Newtonsoft.Json;
using Rhino;
using System.Drawing;
using System.Text.Json.Serialization;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI
{
    public class LanduseListEditMessage : IMessageFromUI<ModelStateMessage>
    {
        [JsonPropertyName("messageType")]
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.LanduseListEdit;

        [JsonProperty("useEditType")]
        public EditType EditType { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("color")]
        public string Color { get; init; }

        [JsonProperty("id")]
        public Guid Id { get; init; }

        public ModelStateMessage HandleMessage()
        {
            var data = new LanduseData(Name, ColorTranslator.FromHtml(Color), Id);

            LanduseActions.Run([data], EditType);
            ModelBuilderPipelines.RhinoOutputPipeline(
                RhinoDoc.ActiveDoc,
                RhinoEventHandler.HTMLViewModel.AppMode,
                true);

            return new ModelStateMessage();
        }
    }

   
}
