using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;

namespace ArchSitePlan.Core.ModelEditActions;
public class RefreshAllZoneContentsAction:IModelEditAction
{
    public void Run()
    {
        var zones = CoreModelSingelton.Instance.Zones;
        
        foreach (var zone in zones)
        {
            var boundary = zone.Boundary;

            // This indicates that the zone is the full model
            if (boundary == null)
            {
                zone.Parcels = CoreModelSingelton.Instance.Parcels;
                zone.Buildings = CoreModelSingelton.Instance.Buildings;
                zone.Transit = CoreModelSingelton.Instance.Roads;
                continue;
            }

            var parcelsWithin = CoreModelSingelton.Instance.Parcels
                .Where(p => boundary.IIsContaining([p.Boundary.ControlPointCentroid()]))
                .ToList();

            var buildingsWithin = CoreModelSingelton.Instance.Buildings
                .Where(bldg => boundary.IIsContaining([bldg.Origin]))
                .ToList();

            var roadsWithin = CoreModelSingelton.Instance.Roads
                .Where(r => boundary.IIsContaining([r.Centerline.PointAtParameter(0.5)]))
                .ToList();

            zone.Parcels = parcelsWithin;
            zone.Buildings = buildingsWithin;
            zone.Transit = roadsWithin;
        }
    }
}
