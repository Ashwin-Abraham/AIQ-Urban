
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Actions;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.HTML_UI.MessageToUI;
using Newtonsoft.Json;
using Rhino;

namespace ArchSitePlan.UI.HTML_UI.MessagesFromUI
{
    public enum UseEditType { Add, Delete, Edit };
    public class LanduseListEditMessage : IMessageFromUI<EmptyMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.LanduseListEdit;

        [JsonProperty("useEditType")]
        public UseEditType EditType { get; init; }

        [JsonProperty("oldUseName")]
        public string OldUseName { get; init; }

        [JsonProperty("oldUseColor")]
        public string OldUseHtmlColor { get; init; }

        [JsonProperty("newUseName")]
        public string NewUseName { get; init; }

        [JsonProperty("newUseColor")]
        public string NewUseHtmlColor { get; init; }

        public EmptyMessage HandleMessage()
        {
            var doc = RhinoDoc.ActiveDoc;
            var appMode = RhinoEventHandler.HTMLViewModel.AppMode;

            switch (EditType)
            {
                case UseEditType.Add: AppLanduses.AddUse(NewUseName, NewUseHtmlColor);
                    break;

                case UseEditType.Edit: 
                    AppLanduses.ReplaceUse(OldUseName, NewUseName, NewUseHtmlColor);
                    // Simple reloading is fine because the Landuse object is modified
                    // Therefore any objects carrying it will also have changed
                    ModelBuilderPipelines.ReloadAppPipeline(doc, appMode);
                    break;

                case UseEditType.Delete:
                    // We can only remove Landuse from the list of uses, not destroy it
                    // Therefore the old use needs to also be swapped out with Undefined
                    ModelBuilderPipelines.DeleteParcelUseAndReassignUseBasedObjs(doc, OldUseName);
                    break;

                default:break;
            }

            // Modify core model and outputted model parcels and buildings
            ModelBuilderPipelines.ReloadAppPipeline(RhinoDoc.ActiveDoc,
                RhinoEventHandler.HTMLViewModel.AppMode);

            return new EmptyMessage();
        }
    }

   
}
