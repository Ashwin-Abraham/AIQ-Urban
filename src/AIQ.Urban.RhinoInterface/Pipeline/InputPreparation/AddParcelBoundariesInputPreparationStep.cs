using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class AddParcelBoundariesInputPreparationStep(IEnumerable<Guid> objectIds) : IInputPreparationStep
{
    private readonly List<Guid> _objectIds = objectIds.Distinct().ToList();

    public List<ICreationData> Run(InputPreparationContext context)
    {
        var objects = RhinoObjectResolver.Resolve(context.RhinoDoc, _objectIds);
        return new SelectParcelBoundariesStep(objects, context.LoggerFactory).Run(context.RhinoDoc);
    }
}
