using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class ResetUrbanElementsStep : IDeleteUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, ICreationData? creationData)
    {
        var ids = model.GetUrbanElements()
            .SelectMany(e => e.GetGeometryIds())
            .Concat(model.TransitSurfaces.Select(ts => ts.Profile.Id))
            .ToList();

        model.Reset();

        return ids;
    }
}
