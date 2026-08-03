using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class RejectParcelReplacementInputPreparationStep(
    Guid oldGeometryId,
    Guid replacementRhinoObjectId) : IInputPreparationStep
{
    public List<ICreationData> Run(InputPreparationContext context)
    {
        var parcel = UrbanModelInputResolver.FindByGeometryId<Parcel>(context.UrbanModel, oldGeometryId);
        return parcel is null
            ? []
            : [new RhinoObjectRestorationData(replacementRhinoObjectId, parcel.GetGeometryIds())];
    }
}
