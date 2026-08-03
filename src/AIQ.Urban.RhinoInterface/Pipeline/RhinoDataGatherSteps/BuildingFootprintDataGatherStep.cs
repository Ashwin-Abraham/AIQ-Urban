using AIQ.Urban.Core.ModelActions.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class BuildingFootprintDataGatherStep(List<BuildingFootprintData> buildingData) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) => buildingData.Cast<IModelEditData>().ToList();
}
