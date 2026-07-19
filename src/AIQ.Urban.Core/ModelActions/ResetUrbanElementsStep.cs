using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class ResetUrbanElementsStep : IUrbanModelStep
{
    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var ids = model.GetUrbanElements()
            .SelectMany(e => e.GetGeometryIds())
            .Concat(model.TransitSurfaces.Select(ts => ts.Profile.Id))
            .ToList();

        model.Reset();

        return new([], ids);
    }
}
