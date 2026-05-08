using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Models.CreationData;

namespace ArchSitePlan.Core.ModelEditActions.BuildingActions;
public class AddBuildingAction : IModelEditAction
{
    public static void Run(List<BuildingData> configs)
    {
        var ids = configs.Select(x => x.EnvelopeId).ToList();

        DeleteBuildingAction.Run(ids);

        foreach (var config in configs)
        {
            if (config.Envelope == null)
                continue;

            var floorplates = config.Floors.Select(x => CreateFloorplate(x)).ToList();
            var newBldg = new Building(
                config.Envelope,
                floorplates,
                config.FloorHt,
                config.EnvelopeId,
                config.BaseFloorId);

            CoreModelSingelton.Instance.Buildings.Add(newBldg);
        }
    }

    private static FloorPlate CreateFloorplate(FloorplateData data)
    {
        var use = CoreModelSingelton.Instance.Landuses.Find(x => data.Use != null && x.Name == data.Use.Name);
        return new FloorPlate(data.Geometry, data.Id, use);
    }

}