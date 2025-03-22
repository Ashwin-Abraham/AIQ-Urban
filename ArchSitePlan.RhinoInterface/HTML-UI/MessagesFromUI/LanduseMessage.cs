using ArchSitePlan.Core.Models;
using ArchSitePlan.RhinoInterface.Actions;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.Helpers;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Rhino;
using Rhino.DocObjects;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI
{
    public class LanduseMessage: IMessageFromUI<StudyAreaMetricsMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Landuse;
        public string Use { get; init; }
        public string Category { get; init; }

        public StudyAreaMetricsMessage HandleMessage()
        {
            if(!AppLanduses.TryGetUse(Use, out var use))
                throw new Exception("Invalid land use received from UI");

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
                RhinoEventHandler.HTMLViewModel.AppMode);
                
            return new();
        }
    }
}
