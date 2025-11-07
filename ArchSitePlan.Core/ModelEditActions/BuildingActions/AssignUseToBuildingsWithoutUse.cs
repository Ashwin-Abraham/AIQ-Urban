using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;

namespace ArchSitePlan.Core.ModelEditActions.BuildingActions;
public class AssignUseToFloorplatesWithoutUse : IModelEditAction
{
    public void Run()
    {
        foreach(var building in CoreModelSingelton.Instance.Buildings)
        {
            Parcel? hostParcel = null;

            foreach (var floorplate in building.FloorPlates)
            {
                bool isUnassigned = floorplate.Use == null || floorplate.Use == AppLanduses.Undefined;

                if(!isUnassigned)
                    continue;

                if(hostParcel == null)
                {
                    hostParcel = CoreModelSingelton.Instance.Parcels
                        .Where(p => p.Boundary.ExternalBoundary.IIsContaining([building.Origin]))
                        .MinBy(x => x.Area());

                    building.HostParcelCoreModelId = hostParcel?.CoreModelId?? Guid.Empty;
                }

                if (hostParcel == null)
                {
                    floorplate.Use = AppLanduses.Undefined;
                }
                else
                {
                    floorplate.Use = hostParcel.LandUse;
                }
            }
        }
        
    }
}
