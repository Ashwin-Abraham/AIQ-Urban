using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;

namespace ArchSitePlan.Core.ModelEditActions;
public class FloorplateEditUseAction : IModelEditAction
{
    public void Run(List<FloorplateData> configs)
    {
        configs.RemoveAll(c => c.Use == null);

        var floorplates = CoreModelSingelton.Instance.Buildings.SelectMany(x => x.FloorPlates).ToList();

        foreach (var floorplate in floorplates)
        {
            var config = configs.Find(c => c.Id == floorplate.Id);

            if (config == null)
                continue;

            floorplate.Use = config.Use;
        }
    }
}
