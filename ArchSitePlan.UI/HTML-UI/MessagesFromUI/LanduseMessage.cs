
using ArchSitePlan.UI.Actions;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.HTML_UI.MessageToUI;
using Eto.Forms;
using Rhino;
using Rhino.DocObjects;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.UI.HTML_UI.MessagesFromUI
{
    public class LanduseMessage: IMessageFromUI<MetricsMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Landuse;
        public string Use { get; init; }
        public string Category { get; init; }

        public MetricsMessage HandleMessage()
        {
            try
            {
                if (!Enum.TryParse<Landuse>(
                    Use,
                    true,
                    out var use))
                {
                    //throw new Exception("Invalid landuse received from UI");
                    use = Landuse.Civic;
                }

                var vm = RhinoEventHandler.WPFViewModel;
                RhinoDoc doc = RhinoDoc.ActiveDoc;
                int parcelLayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                    doc,
                    LayerConstants.ParcelsLayer);

                // Try to select a surface
                var srfObjs = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)
                    .Where(obj => obj.ObjectType == ObjectType.Brep
                    || obj.ObjectType == ObjectType.Surface)
                    .ToList();

                if (srfObjs.Count == 0)
                    throw new Exception(UIMessages.ChangeLandUseInstruction);

                ModelBuilderPipelines.ChangeParcelUsePipeline(doc,
                    srfObjs.Select(s => s.Id).ToList(),
                    use,
                    vm.AppMode);

                
                RhinoEventHandler.HTMLViewModel.UpdateGraphMessage();
                return RhinoEventHandler.HTMLViewModel.MetricsMessageData;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return RhinoEventHandler.HTMLViewModel.MetricsMessageData;
            }
        }
    }
}
