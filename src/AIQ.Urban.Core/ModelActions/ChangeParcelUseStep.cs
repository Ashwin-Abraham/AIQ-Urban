using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;

namespace AIQ.Urban.Core.ModelActions;

public class ChangeParcelUseStep : IUrbanModelStep
{
    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var data = creationData.OfType<ParcelUseChangeData>().First();
        var geometryIds = data.GeometryIds;
        var newUse = data.NewUse;

        var oldIds = new List<Guid>();
        var newIds = new List<Guid>();

        foreach (var parcel in model.Parcels)
        {
            if (geometryIds.Contains(parcel.Outline.Id))
            {
                oldIds.Add(parcel.Outline.Id);
                parcel.SetUse(newUse);
                newIds.Add(parcel.Outline.Id);
            }
        }

        return new(newIds, oldIds);
    }
}
