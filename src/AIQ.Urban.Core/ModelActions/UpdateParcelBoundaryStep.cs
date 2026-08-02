using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.UrbanElements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class UpdateParcelBoundaryStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<UpdateParcelBoundaryStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var data = creationData.OfType<ParcelBoundaryUpdateData>().FirstOrDefault();
        if (data is null)
            return new([], []);

        var oldBoundary = model.ParcelBoundaries.FirstOrDefault(x => x.Id == data.OldBoundaryId);
        if (oldBoundary is null)
            return new([], []);

        model.ParcelBoundaries.Remove(oldBoundary);
        var newBoundary = new ParcelBoundary(data.NewBoundary);
        model.ParcelBoundaries.Add(newBoundary);

        _logger.LogInformation("Updated parcel boundary {Id}", data.OldBoundaryId);
        return new([newBoundary.Id], [oldBoundary.Id]);
    }
}
