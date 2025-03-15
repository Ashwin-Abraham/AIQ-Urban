using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.UI.Actions;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.HTML_UI.MessageToUI;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Geometry;
using Eto.Forms;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI.HTML_UI.MessagesFromUI
{
    public class DividerMessage:IMessageFromUI<MetricsMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Divider;

        public MetricsMessage HandleMessage()
        {
            RhinoEventHandler.HTMLViewModel.AppMode = ModellingTool.Dividers;

            RhinoDoc doc = RhinoDoc.ActiveDoc;
            int parcelDividerLayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                doc,
                LayerConstants.ParcelDividerLayer,
                LayerConstants.ParcelDividerColour);

            // Try to select a curve
            var rhinoObjs = doc.Objects.GetSelectedObjects(false, false).ToList();
            if (rhinoObjs.Count == 0)
                throw new Exception(UIMessages.AddDividerInstruction);

            var rhinoCrvs = rhinoObjs
                .Where(obj => obj.ObjectType == ObjectType.Curve
                    && (obj.Geometry as Curve)!.Degree < 2)
                .ToList();

            int removed = rhinoObjs.Count() - rhinoCrvs.Count;
                
            List<DividerData> dividerData = new();

            foreach (var rhinoObj in rhinoCrvs)
            {
                var bhomCrv = (rhinoObj.Geometry as Curve)!.ToBHoM2DegreeCrv();
                if (bhomCrv is null || bhomCrv.IIsSelfIntersecting())
                {
                    removed++;
                    continue;
                }

                dividerData.Add(new(bhomCrv, rhinoObj.Id));
                LayerHelpers.ChangeLayer(rhinoObj, parcelDividerLayerIndex);
            }

            if (removed > 0)
                MessageBox.Show($"Removed {removed} objects from selection that were either" +
                    $"\n 1. Not lines or polylines " +
                    $"\n 2. Self intersecting");

            var vm = RhinoEventHandler.HTMLViewModel;
            if (!dividerData.Any()) return vm.MetricsMessageData;

            ModelBuilderPipelines.AddOrUpdateParcelDividerPipeline(doc, dividerData, ModellingTool.Dividers);

            vm.UpdateGraphMessage();
            return vm.MetricsMessageData;
        }
    }
}
