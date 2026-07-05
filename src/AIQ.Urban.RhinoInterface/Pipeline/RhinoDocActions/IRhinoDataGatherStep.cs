using AIQ.Urban.Core.ModelActions.CreationData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;
internal interface IRhinoDataGatherStep
{
    internal ICreationData Run(RhinoDoc doc);
}
