using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class ChangeUseInputPreparationStep(
    IEnumerable<Guid> geometryIds,
    string useName) : IInputPreparationStep
{
    private readonly List<Guid> _geometryIds = geometryIds.Distinct().ToList();

    public List<ICreationData> Run(InputPreparationContext context)
    {
        var use = context.UrbanModel.GetUse(useName);
        return
        [
            new ParcelUseChangeData(_geometryIds, use),
            new BuildingUseChangeData(_geometryIds, use),
        ];
    }
}
