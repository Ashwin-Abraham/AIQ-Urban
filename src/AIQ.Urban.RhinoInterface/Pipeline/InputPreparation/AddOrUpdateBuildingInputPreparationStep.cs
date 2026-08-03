using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class AddOrUpdateBuildingInputPreparationStep(
    IEnumerable<Guid> objectIds,
    double floorHeight,
    int floorCount,
    string useName,
    BuildingInputMode mode) : IInputPreparationStep
{
    private readonly List<Guid> _objectIds = objectIds.Distinct().ToList();

    public List<ICreationData> Run(InputPreparationContext context)
    {
        var use = context.UrbanModel.GetUse(useName);
        var objects = RhinoObjectResolver.Resolve(context.RhinoDoc, _objectIds);

        return mode switch
        {
            BuildingInputMode.Footprint => PrepareFootprints(context, objects, use),
            BuildingInputMode.Mass => PrepareMasses(context, objects, use),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };
    }

    private List<ICreationData> PrepareFootprints(
        InputPreparationContext context,
        IEnumerable<Rhino.DocObjects.RhinoObject> objects,
        Core.Models.Use use)
    {
        var data = new SelectBuildingFootprintStep(
            objects,
            floorHeight,
            floorCount,
            use,
            context.LoggerFactory).Run(context.RhinoDoc);

        var deletionData = data
            .OfType<BuildingFootprintData>()
            .Select(x => new RhinoObjectDeletionData(x.Footprint.Id))
            .ToList();
        data.AddRange(deletionData);
        return data;
    }

    private List<ICreationData> PrepareMasses(
        InputPreparationContext context,
        IEnumerable<Rhino.DocObjects.RhinoObject> objects,
        Core.Models.Use use)
    {
        var selected = new SelectBuildingEnvelopeStep(
            objects,
            floorHeight,
            use,
            context.LoggerFactory)
            .Run(context.RhinoDoc)
            .OfType<BuildingMassData>()
            .ToList();

        var data = new BuildingEnvelopeCreationDataResolver(context.UrbanModel).Resolve(selected);
        data.AddRange(selected.Select(x => new RhinoObjectDeletionData(x.Envelope.Id)));
        return data;
    }
}
