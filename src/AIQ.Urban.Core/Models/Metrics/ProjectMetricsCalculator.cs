using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.Metrics;

/// <summary>Computes <see cref="ProjectMetrics"/> from the current state of an <see cref="UrbanModel"/> and stores it on <see cref="UrbanModel.ProjectMetrics"/>.</summary>
public static class ProjectMetricsCalculator
{
    /// <summary>Compute project metrics and store the result on <paramref name="urbanModel"/>.</summary>
    /// <param name="urbanModel">The model to read elements from and write <see cref="UrbanModel.ProjectMetrics"/> to.</param>
    /// <param name="containmentTolerance">Tolerance used for point-in-polygon containment checks when matching buildings to parcels.</param>
    public static void CalculateAndStore(UrbanModel urbanModel, double containmentTolerance)
    {
        var buildingMetrics = urbanModel.Buildings
            .Select(b => new BuildingMetrics(b))
            .ToList();

        var parcelMetrics = urbanModel.Parcels
            .Select(p => new ParcelMetrics(p, FindBuildingsOnParcel(p, urbanModel, buildingMetrics, containmentTolerance)))
            .ToList();

        urbanModel.ProjectMetrics = new ProjectMetrics(buildingMetrics, parcelMetrics);
    }

    /// <summary>Find which <see cref="BuildingMetrics"/> instances sit inside a given parcel's outline.</summary>
    private static List<BuildingMetrics> FindBuildingsOnParcel(
        Parcel parcel,
        UrbanModel urbanModel,
        List<BuildingMetrics> buildingMetrics,
        double tolerance)
    {
        var parcelOutline = parcel.Outline.Geometry;
        parcelOutline.TryGetPlane(out var plane);

        var results = new List<BuildingMetrics>();

        for (int i = 0; i < urbanModel.Buildings.Count; i++)
        {
            var center = urbanModel.Buildings[i].GetRepresentativeCenter();
            var point = new Point3d(center.X, center.Y, plane.OriginZ);

            if (parcelOutline.Contains(point, plane, tolerance) == PointContainment.Inside)
                results.Add(buildingMetrics[i]);
        }

        return results;
    }
}
