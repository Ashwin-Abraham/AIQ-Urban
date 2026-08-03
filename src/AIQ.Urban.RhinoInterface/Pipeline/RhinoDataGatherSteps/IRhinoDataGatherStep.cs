using AIQ.Urban.Core.ModelActions.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
public interface IRhinoDataGatherStep
{
    List<IModelEditData> Run(RhinoDoc doc);
}
