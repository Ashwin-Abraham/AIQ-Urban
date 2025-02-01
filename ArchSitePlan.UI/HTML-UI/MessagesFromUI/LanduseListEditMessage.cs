
using ArchSitePlan.UI.Actions;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.HTML_UI.MessageToUI;
using ArchSitePlan.UI.InputValidation.Results;
using ArchSitePlan.UI.InputValidation.Validators;
using ArchSitePlan.UI.Models;
using Eto.Forms;
using Newtonsoft.Json;
using Rhino;
using Rhino.DocObjects;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI.HTML_UI.MessagesFromUI
{
    public class LanduseListEditMessage : IMessageFromUI<EmptyMessage>
    {

        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.LanduseListEdit;
        
        [JsonProperty("oldUse")]
        public string OldUse { get; init; }
        
        [JsonProperty("newUse")]
        public double NewUse { get; init; }

        public EmptyMessage HandleMessage()
        {
            return new EmptyMessage();
        }
    }

   
}
