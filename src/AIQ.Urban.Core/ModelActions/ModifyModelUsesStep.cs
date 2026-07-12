using System.Drawing;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;

namespace AIQ.Urban.Core.ModelActions;

public class ModifyModelUsesStep(
    UseEditType editType,
    string name,
    Color color,
    Guid id) : IEditUrbanElementsStep
{
    public Use? AffectedUse { get; private set; }

    public EditStepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        switch (editType)
        {
            case UseEditType.Add:
                return Add(model);

            case UseEditType.Edit:
                return Edit(model);

            case UseEditType.Remove:
                return Remove(model);

            default:
                throw new ArgumentOutOfRangeException(nameof(editType), editType, null);
        }
    }

    private EditStepResult Add(UrbanModel model)
    {
        var use = new Use(id, name, color);
        model.Uses.Add(use);
        AffectedUse = use;
        return new([], []);
    }

    private EditStepResult Edit(UrbanModel model)
    {
        var use = model.Uses.FirstOrDefault(u => u.Id == id)
            ?? throw new ArgumentException($"Use with Id '{id}' not found.");

        if (IsSystemUse(model, use))
            return new([], []);

        use.Name = name;
        use.Color = color;
        AffectedUse = use;

        var geometryIds = GatherIdsByUse(model, use);
        return new(geometryIds, geometryIds);
    }

    private EditStepResult Remove(UrbanModel model)
    {
        var use = model.Uses.FirstOrDefault(u => u.Id == id)
            ?? throw new ArgumentException($"Use with Id '{id}' not found.");

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
        AffectedUse = undefinedUse;
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
