using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models.UrbanGraph;

namespace AIQ.Urban.Core.ModelActions;

public class GraphCreationStep(double _approachTolerance = 1e-5, double _tolerance = 1e-5) : IUrbanModelStep
{
    public StepResult Run(UrbanModel model, List<IModelEditData> creationData)
    {
        model.UrbanGraph = new UrbanGraphBuilder(_approachTolerance, _tolerance)
            .WithTransit(model.Transits)
            .WithParcels(model.Parcels)
            .WithBuildings(model.Buildings)
            .Run();

        return new([],[]);
    }
}
