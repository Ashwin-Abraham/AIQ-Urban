using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.RhinoInterface.Pipeline.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class TransitAdditionGatherStep(List<TransitData> transitData, List<Guid> centerlinesToRemove) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc)
    {
        var result = transitData.Cast<IModelEditData>().ToList();
        result.Add(new TransitDeletionData(centerlinesToRemove));
        result.AddRange(centerlinesToRemove.Select(id => new RhinoObjectDeletionData(id)));
        return result;
    }
}
