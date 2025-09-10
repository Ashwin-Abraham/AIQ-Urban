using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.RhinoBhomInterop;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.Helpers;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using BH.Engine.Geometry;
using Eto.Forms;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI
{
    public class DividerMessage:IMessageFromUI<StudyAreaMetricsMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Divider;

        public StudyAreaMetricsMessage HandleMessage()
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
            if (!dividerData.Any()) return new();

            ModelBuilderPipelines.AddOrUpdateParcelDividerPipeline(dividerData);

            return new();
        }
    }
}
