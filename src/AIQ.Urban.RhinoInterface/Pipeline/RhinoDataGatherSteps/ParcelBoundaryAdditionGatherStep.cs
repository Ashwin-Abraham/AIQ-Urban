using AIQ.Urban.Core.ModelActions.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class ParcelBoundaryAdditionGatherStep(List<ParcelBoundaryData> boundaryData) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) => boundaryData.Cast<IModelEditData>().ToList();
}
