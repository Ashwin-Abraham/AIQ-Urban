using ArchSitePlan.Core.Models;

namespace ArchSitePlan.Core.ModelEditActions.BuildingActions;
public class FloorplateEditUseAction : IModelEditAction
{
    public void Run(List<(Guid FloorplateId, Landuse Use)> configs)
    {
        var floorplates = CoreModelSingelton.Instance.Buildings.SelectMany(x => x.FloorPlates).ToList();

        foreach (var floorplate in floorplates)
        {
            var config = configs.Find(c => c.FloorplateId == floorplate.Id);

            if (config == default)
                continue;

            floorplate.Use = config.Use;
        }
    }
}
