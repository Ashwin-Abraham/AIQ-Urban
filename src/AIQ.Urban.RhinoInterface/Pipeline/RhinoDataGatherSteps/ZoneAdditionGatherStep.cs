using AIQ.Urban.Core.ModelActions.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class ZoneAdditionGatherStep(List<ZoneData> zoneData) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) => zoneData.Cast<IModelEditData>().ToList();
}
