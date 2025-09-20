using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Rhino;
using Rhino.DocObjects;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;

public class ChangeParcelLanduseMessage: IMessageFromUI<ModelStateMessage>
{
    public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Landuse;
    public string Use { get; init; }
    public string Category { get; init; }

    public ModelStateMessage HandleMessage()
    {
        RhinoDoc doc = RhinoDoc.ActiveDoc;
        
        // Try to select a surface
        var srfObjs = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)
            .Where(obj => obj.ObjectType == ObjectType.Brep
            || obj.ObjectType == ObjectType.Surface)
            .ToList();

        if (srfObjs.Count == 0)
            throw new Exception(UIMessages.ChangeLandUseInstruction);

        ModelBuilderPipelines.ChangeParcelUsePipeline(
            srfObjs.Select(s => s.Id).ToList(),
            Use);

        ModelBuilderPipelines.RhinoOutputPipeline(doc, RhinoEventHandler.HTMLViewModel.AppMode, true);
        return new();
    }
}
