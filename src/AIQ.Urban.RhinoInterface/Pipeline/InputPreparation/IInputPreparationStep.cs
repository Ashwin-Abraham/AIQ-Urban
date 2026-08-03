using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public interface IInputPreparationStep
{
    List<ICreationData> Run(InputPreparationContext context);
}
