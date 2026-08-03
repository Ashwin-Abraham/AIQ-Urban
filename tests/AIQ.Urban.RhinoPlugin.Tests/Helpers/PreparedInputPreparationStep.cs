using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

namespace AIQ.Urban.RhinoInterface.Tests.Helpers;

internal class PreparedInputPreparationStep(IEnumerable<ICreationData> data = null) : IInputPreparationStep
{
    private readonly List<ICreationData> _data = data?.ToList() ?? [];

    public List<ICreationData> Run(InputPreparationContext context)
    {
        return [.. _data];
    }
}
