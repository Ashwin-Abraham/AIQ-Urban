using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using ArchSitePlan.RhinoInterface.InputValidation.Results;
using ArchSitePlan.RhinoInterface.InputValidation.Validators;
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
        var allowedTypes = new List<ObjectType>
        {
            ObjectType.Curve, ObjectType.Brep, ObjectType.Mesh, ObjectType.Extrusion
        };

        // Try to select a surface
        var rhinoObjs = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)
            .Where(obj => allowedTypes.Contains(obj.ObjectType))
            .ToList();

        var validation = new UseChangeValidator()
            .ValidateInput(rhinoObjs);

        if (validation.Result != ValidationResult.All_Valid)
            throw new Exception(validation.Message);

        ModelBuilderPipelines.ChangeObjectUsePipeline(
            rhinoObjs.Select(s => s.Id).ToList(),
            Use);

        ModelBuilderPipelines.RhinoOutputPipeline(doc, RhinoEventHandler.HTMLViewModel.AppMode, true);
        return ModelBuilderPipelines.GenerateModelStateMessage();
    }
}
