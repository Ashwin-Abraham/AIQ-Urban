using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;

namespace AIQ.Urban.Core.ModelActions;

public class ChangeParcelUseStep(
    List<Guid> parcelGeometryIds,
    Use newUse) : IEditUrbanElementsStep
{
    public EditStepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var oldIds = new List<Guid>();
        var newIds = new List<Guid>();

        foreach (var parcel in model.Parcels)
        {
            if (parcelGeometryIds.Contains(parcel.Outline.Id))
            {
                oldIds.Add(parcel.Outline.Id);
                parcel.SetUse(newUse);
                newIds.Add(parcel.Outline.Id);
            }
        }

        return new(newIds, oldIds);
    }
}
