using AIQ.Urban.Core.ModelActions.CreationData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
public interface IRhinoDataGatherStep
{
    List<ICreationData> Run(RhinoDoc doc);
}
