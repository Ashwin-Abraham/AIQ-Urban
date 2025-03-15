using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.UI.Actions;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.HTML_UI.MessageToUI;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI.HTML_UI.MessagesFromUI;

public class TransitMessage : IMessageFromUI<MetricsMessage>
{
    public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Transit;
    public double RoadWidth { get; init; }
    public double RightPavementWidth { get; init; }
    public double LeftPavementWidth { get; init; }
    public MetricsMessage HandleMessage()
    {
        RhinoEventHandler.HTMLViewModel.AppMode = ModellingTool.Transit;

        RhinoDoc doc = RhinoDoc.ActiveDoc;
        int roadLayerIndex = LayerHelpers.CreateOrGetLayerIndex(
            doc,
            LayerConstants.RoadCLLayer,
            LayerConstants.RoadColour);

        // Try to select a curve
        var rhinoObjs = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (rhinoObjs.Count == 0)
            throw new Exception(UIMessages.RoadInstruction);

        int roadIndex = RhinoObjAttributeConstants.RoadGeometryAttributes.LayerIndex;

        List<TransitData> transitData = new();

        foreach (var rhinoObj in rhinoObjs)
        {
            var curve = rhinoObj.Geometry as Curve;
            if (curve == null)
                throw new Exception(UIMessages.RoadInstruction);

            var bhomCrv = curve.ToBHoM2DegreeCrv();
            if (bhomCrv == null || bhomCrv.IIsSelfIntersecting())
                throw new Exception(UIMessages.RoadInstruction);

            transitData.Add(new(bhomCrv, rhinoObj.Id, RoadWidth));
            LayerHelpers.ChangeLayer(rhinoObj, roadLayerIndex);
        }

        var vm = RhinoEventHandler.HTMLViewModel;
        if (!transitData.Any()) return vm.MetricsMessageData;

        ModelBuilderPipelines.AddOrUpdateRoadPipeline(doc, transitData, ModellingTool.Transit);

        vm.UpdateGraphMessage();
        return vm.MetricsMessageData;
    }
}