using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public record StepResult(List<Guid> AddedIds, List<Guid> DeletedIds);

public interface IUrbanModelStep
{
    public StepResult Run(UrbanModel model, List<ICreationData> creationData);
}
