using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.RhinoInterface.Pipeline.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class BuildingEnvelopeDataGatherStep(
    List<BuildingMassData> envelopeData,
    UrbanModel urbanModel,
    List<Guid> sourceRhinoObjectIds) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc)
    {
        var result = new BuildingEnvelopeCreationDataResolver(urbanModel).Resolve(envelopeData);
        result.AddRange(sourceRhinoObjectIds.Select(id => new RhinoObjectDeletionData(id)));
        return result;
    }
}
