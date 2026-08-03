using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class ChangeBuildingUseInputPreparationStep(
    IEnumerable<Guid> geometryIds,
    string useName) : IInputPreparationStep
{
    private readonly List<Guid> _geometryIds = geometryIds.Distinct().ToList();

    public List<ICreationData> Run(InputPreparationContext context)
    {
        var use = context.UrbanModel.GetUse(useName);
        return [new BuildingUseChangeData(_geometryIds, use)];
    }
}
