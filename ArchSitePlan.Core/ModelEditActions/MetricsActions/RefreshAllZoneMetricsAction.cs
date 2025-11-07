using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;

namespace ArchSitePlan.Core.ModelEditActions.MetricsActions;
public class RefreshAllZoneMetricsAction
{
    public List<(StudyArea Study, StudyAreaMetrics Metrics)> Run()
    {
        var zones = CoreModelSingelton.Instance.Zones;

        List<(Parcel Parcel, ParcelMetrics Metrics)> allParcelMetrics = [];
        
        foreach(var parcel in CoreModelSingelton.Instance.Parcels)
        {
            var buildingsInParcel = CoreModelSingelton.Instance.Buildings
                .Where(b => parcel.Boundary.ExternalBoundary.IIsContaining([b.Origin]))
                .ToList();

            allParcelMetrics.Add(new CalculateParcelMetricsAction(parcel, buildingsInParcel).Run());
        }

        List<(Building Building, BuildingMetrics Metrics)> allBuildingMetrics = new CalculateBuildingMetricsAction(CoreModelSingelton.Instance.Buildings).Run();
        List<(Road Road, TransitMetrics Metrics)> allTransitMetrics = new CalculateTransitMetricsAction(CoreModelSingelton.Instance.Roads).Run();

        List<(StudyArea, StudyAreaMetrics)> zoneMetrics = [];

        foreach (var zone in zones)
        {
            var boundary = zone.Boundary;
            var parcelMetrics = allParcelMetrics;
            var buildingMetrics = allBuildingMetrics;
            var transitMetrics = allTransitMetrics;

            if (boundary != null)
            {
                parcelMetrics = allParcelMetrics
                .Where(p => boundary.IIsContaining([p.Parcel.Boundary.ControlPointCentroid()]))
                .ToList();

                buildingMetrics = allBuildingMetrics
                    .Where(bldg => boundary.IIsContaining([bldg.Building.Origin]))
                    .ToList();

                transitMetrics = allTransitMetrics
                    .Where(r => boundary.IIsContaining([r.Road.Centerline.PointAtParameter(0.5)]))
                    .ToList();
            }

            zoneMetrics.Add((
                zone, 
                new CalculateZoneMetricsAction(parcelMetrics, transitMetrics, buildingMetrics).Run(zone)));
        }

        return zoneMetrics;
    }
}