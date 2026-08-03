using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class DeleteAllUrbanElementsInputPreparationStep : IInputPreparationStep
{
    public List<ICreationData> Run(InputPreparationContext context)
    {
        return
        [
            new TransitDeletionData(context.UrbanModel.Transits.Select(x => x.Centerline.Id).ToList()),
            new BuildingDeletionData(context.UrbanModel.Buildings.SelectMany(x => x.GetGeometryIds()).ToList()),
            new ZoneDeletionData(context.UrbanModel.Zones.SelectMany(x => x.GetGeometryIds()).ToList()),
            new ParcelBoundaryDeletionData(context.UrbanModel.ParcelBoundaries.Select(x => x.Id).ToList()),
        ];
    }
}
