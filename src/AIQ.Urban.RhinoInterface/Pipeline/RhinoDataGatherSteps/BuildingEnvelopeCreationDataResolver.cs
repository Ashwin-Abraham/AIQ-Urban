using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.EditData;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class BuildingEnvelopeCreationDataResolver(UrbanModel _urbanModel)
{
    public List<IModelEditData> Resolve(IEnumerable<BuildingMassData> selectedEnvelopes)
    {
        return selectedEnvelopes
            .Select(Resolve)
            .ToList();
    }

    private IModelEditData Resolve(BuildingMassData selectedEnvelope)
    {
        var existingBuilding = _urbanModel.Buildings
            .FirstOrDefault(building => building.Envelope.Id == selectedEnvelope.Envelope.Id);

        if (existingBuilding is null)
            return selectedEnvelope;

        var oldPlateUses = existingBuilding.FloorPlates
            .OrderBy(plate => plate.Z())
            .Select(plate => plate.Use)
            .ToList();

        return new BuildingUpdateData(
            existingBuilding.Envelope.Id,
            selectedEnvelope.Envelope.Geometry,
            selectedEnvelope.FloorHeight,
            oldPlateUses);
    }
}
