using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class AddZoneInputPreparationStep(IEnumerable<Guid> boundaryObjectIds, string name) : IInputPreparationStep
{
    private readonly List<Guid> _boundaryObjectIds = boundaryObjectIds.Distinct().ToList();

    public List<ICreationData> Run(InputPreparationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Contains("::", StringComparison.Ordinal))
            throw new ArgumentException("Zone name must not contain '::'.", nameof(name));

        if (context.UrbanModel.Zones.Any(
                zone => string.Equals(zone.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return [new InputRejectionData(
                "Rhino zone boundary",
                0,
                $"A zone named '{name}' already exists.")];
        }

        var objects = RhinoObjectResolver.Resolve(context.RhinoDoc, _boundaryObjectIds);
        if (objects.Count != 1)
            throw new ArgumentException("Select exactly one zone boundary curve first.");

        return new SelectZoneBoundaryStep(objects, name, context.LoggerFactory).Run(context.RhinoDoc);
    }
}
