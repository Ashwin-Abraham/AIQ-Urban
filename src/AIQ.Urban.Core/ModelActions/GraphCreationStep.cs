using AIQ.Urban.Core.Models.UrbanGraph;

namespace AIQ.Urban.Core.ModelActions;

internal class GraphCreationStep(double _approachTolerance = 1e-5, double _tolerance = 1e-5) : IEditUrbanElementsStep
{
    public List<Guid> Run(UrbanModel model)
    {
        var graphables = model.Transits
            .Cast<IGraphableAsEdge>()
            .ToList();

        var graph = GraphBuilder.Run(graphables);
    }
}
