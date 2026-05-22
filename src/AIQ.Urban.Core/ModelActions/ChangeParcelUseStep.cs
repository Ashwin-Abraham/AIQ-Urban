using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions;

public class ChangeParcelUseStep(
    List<Guid> parcelGeometryIds,
    Use newUse) : IEditUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model)
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

        return oldIds.Concat(newIds).ToList();
    }
}
