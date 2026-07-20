using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class ModifyModelUsesStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ModifyModelUsesStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var data = creationData.OfType<UseModificationData>().First();

        _logger.LogInformation("Modified model uses: {EditType} '{Name}'", data.EditType, data.Name);

        switch (data.EditType)
        {
            case UseEditType.Add:
                return Add(model, data);

            case UseEditType.Edit:
                return Edit(model, data);

            case UseEditType.Remove:
                return Remove(model, data);

            default:
                throw new ArgumentOutOfRangeException(nameof(data.EditType), data.EditType, null);
        }
    }

    private StepResult Add(UrbanModel model, UseModificationData data)
    {
        var use = new Use(data.Id, data.Name, data.Color);
        model.Uses.Add(use);
        return new([], []);
    }

    private StepResult Edit(UrbanModel model, UseModificationData data)
    {
        var use = model.Uses.FirstOrDefault(u => u.Id == data.Id)
            ?? throw new ArgumentException($"Use with Id '{data.Id}' not found.");

        if (IsSystemUse(model, use))
            return new([], []);

        use.Name = data.Name;
        use.Color = data.Color;

        var geometryIds = GatherIdsByUse(model, use);
        return new(geometryIds, geometryIds);
    }

    private StepResult Remove(UrbanModel model, UseModificationData data)
    {
        var use = model.Uses.FirstOrDefault(u => u.Id == data.Id)
            ?? throw new ArgumentException($"Use with Id '{data.Id}' not found.");

        if (IsSystemUse(model, use))
            return new([], []);

        var undefinedUse = model.GetUse(DefaultUses.UndefinedName);
        var geometryIds = new List<Guid>();

        foreach (var parcel in model.Parcels)
        {
            if (ReferenceEquals(parcel.Use, use))
            {
                parcel.SetUse(undefinedUse);
                geometryIds.Add(parcel.Id);
            }
        }

        foreach (var building in model.Buildings)
        {
            foreach (var plate in building.FloorPlates)
            {
                if (ReferenceEquals(plate.Use, use))
                {
                    plate.SetUse(undefinedUse);
                    geometryIds.Add(plate.Id);
                }
            }
        }

        model.Uses.Remove(use);
        return new(geometryIds, geometryIds);
    }

    private static List<Guid> GatherIdsByUse(UrbanModel model, Use targetUse)
    {
        var geometryIds = new List<Guid>();

        foreach (var parcel in model.Parcels)
        {
            if (ReferenceEquals(parcel.Use, targetUse))
                geometryIds.Add(parcel.Id);
        }

        foreach (var building in model.Buildings)
        {
            foreach (var plate in building.FloorPlates)
            {
                if (ReferenceEquals(plate.Use, targetUse))
                    geometryIds.Add(plate.Id);
            }
        }

        return geometryIds;
    }

    private static bool IsSystemUse(UrbanModel model, Use use)
    {
        var undefinedUse = model.GetUse(DefaultUses.UndefinedName);
        return use.Id == undefinedUse.Id;
    }
}
