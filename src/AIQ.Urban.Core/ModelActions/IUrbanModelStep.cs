using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public interface IUrbanModelStep
{
    public List<Guid> Run(UrbanModel model, List<ICreationData> creationData);
}

public interface IAddUrbanElementsStep : IUrbanModelStep;
public interface IDeleteUrbanElementsStep : IUrbanModelStep;

public record EditStepResult(List<Guid> AddedIds, List<Guid> DeletedIds);

public interface IEditUrbanElementsStep
{
    public EditStepResult Run(UrbanModel model);
}
