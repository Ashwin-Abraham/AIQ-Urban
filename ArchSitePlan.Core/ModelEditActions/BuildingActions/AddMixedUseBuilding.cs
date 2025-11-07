using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Models.CreationData;
using BH.Engine.Geometry;

namespace ArchSitePlan.Core.ModelEditActions.BuildingActions;
public class AddMixedUseBuildings(List<MixedUseBuildingData> configs)
{
    public void Run()
    {
        foreach(var config in configs)
        {
            double buildingHeight = config.EnvelopeMesh.Bounds().ZDomain().Length();
            double floorHeight = buildingHeight/ config.Floors.Count;

            var building = new Building(
                config.EnvelopeMesh,
                config.Floors,
                floorHeight);

            CoreModelSingelton.Instance.Buildings.Add(building);
        }
    }
}
