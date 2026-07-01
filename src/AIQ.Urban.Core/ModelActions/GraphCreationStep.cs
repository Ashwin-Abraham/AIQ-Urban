using AIQ.Urban.Core.Models.UrbanGraph;

namespace AIQ.Urban.Core.ModelActions;

internal class GraphCreationStep(double _approachTolerance = 1e-5, double _tolerance = 1e-5) : IEditUrbanElementsStep
{
    public EditStepResult Run(UrbanModel model)
    {
        model.UrbanGraph = new UrbanGraphBuilder(_approachTolerance, _tolerance)
            .WithTransit(model.Transits)
            .WithParcels(model.Parcels)
            .WithBuildings(model.Buildings)
            .Run();

        return new([],[]);
    }
}
