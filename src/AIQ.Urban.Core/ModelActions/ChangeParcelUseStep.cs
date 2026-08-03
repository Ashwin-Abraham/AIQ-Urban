using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class ChangeParcelUseStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ChangeParcelUseStep>();

    public StepResult Run(UrbanModel model, List<IModelEditData> creationData)
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

        _logger.LogInformation("Changed use of {Count} parcels to {Use}", newIds.Count, newUse.Name);

        return new(newIds, oldIds);
    }
}
