using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class AddTransitInputPreparationStep(
    IEnumerable<Guid> objectIds,
    double width,
    TransitType transitType) : IInputPreparationStep
{
    private readonly List<Guid> _objectIds = objectIds.Distinct().ToList();

    public List<ICreationData> Run(InputPreparationContext context)
    {
        var objects = RhinoObjectResolver.Resolve(context.RhinoDoc, _objectIds);
        var data = new SelectTransitCenterlinesStep(
            objects,
            width,
            transitType,
            context.LoggerFactory).Run(context.RhinoDoc);

        var validIds = data
            .OfType<TransitData>()
            .Select(x => x.Centerline.Id)
            .ToList();

        data.Add(new TransitDeletionData(validIds));
        return data;
    }
}
