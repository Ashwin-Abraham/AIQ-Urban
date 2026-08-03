using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.RhinoInterface.Pipeline.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class ManagedRhinoObjectsDeletionGatherStep(List<int> layerIndices) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc)
    {
        return doc.Objects
            .Where(x => layerIndices.Contains(x.Attributes.LayerIndex))
            .Select(x => new RhinoObjectDeletionData(x.Id))
            .Cast<IModelEditData>()
            .ToList();
    }
}
