using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using AIQ.Urban.RhinoInterface.Utils;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class LoadDocumentInputPreparationStep : IInputPreparationStep
{
    public List<ICreationData> Run(InputPreparationContext context)
    {
        var data = new DeleteAllUrbanElementsInputPreparationStep().Run(context);
        var rootIndex = context.RhinoDoc.Layers.FindByFullPath(LayerConstants.Root, -1);
        if (rootIndex == -1)
            return data;

        var rootLayer = context.RhinoDoc.Layers[rootIndex];
        var managedLayerIndices = rootLayer.GetChildren(true)
            .Append(rootLayer)
            .Select(x => x.Index)
            .ToHashSet();

        data.AddRange(context.RhinoDoc.Objects
            .Where(x => managedLayerIndices.Contains(x.Attributes.LayerIndex))
            .Select(x => new RhinoObjectDeletionData(x.Id)));

        var gatherStep = new DocLoadGatherStep(
            context.UrbanModel,
            context.RhinoDoc.ModelAbsoluteTolerance,
            context.LoggerFactory);
        data.AddRange(gatherStep.Run(context.RhinoDoc));
        return data;
    }
}
