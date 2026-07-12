using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions;

/// <summary>
/// Clears all existing parcels from the model and regenerates them from
/// the holes in each transit type's unioned surface.
///
/// For each new parcel, the nearest old parcel's Use is preserved by matching
/// polygon centroids. Falls back to "Public" use when no
/// old parcels exist (e.g. first run).
///
/// When <see cref="ICreationData"/> contains <see cref="CentroidUseData"/> items,
/// those centroid/use pairs are used instead of computing centroids from
/// <c>model.Parcels</c>. This supports doc loading scenarios where parcel
/// uses must be preserved from external data rather than the current model state.
///
/// NOTE: If multiple transit types produce overlapping holes in the same
/// physical area, duplicate parcels will be created. No dedup is performed.
/// </summary>
public class RecalculateParcelsStep : IEditUrbanElementsStep
{
    public EditStepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var oldParcelIds = model.Parcels.SelectMany(p => p.GetGeometryIds()).ToList();

        var externalCentroids = creationData.OfType<CentroidUseData>()
            .Select(c => (c.Centroid, c.Use))
            .ToList();

        var oldCentroids = externalCentroids.Count > 0
            ? externalCentroids
            : CaptureParcelCentroids(model);

        model.Parcels.Clear();

        foreach (var (profile, _) in model.TransitSurfaces)
        {
            foreach (var hole in profile.Holes)
            {
                var use = ResolveUse(hole, oldCentroids, model);
                model.Parcels.Add(new Parcel(hole, use));
            }
        }

        var newParcelIds = model.Parcels.SelectMany(p => p.GetGeometryIds()).ToList();

        return new(newParcelIds, oldParcelIds);
    }

    private static List<(Point3d Centroid, Use Use)> CaptureParcelCentroids(UrbanModel model)
    {
        var centroids = new List<(Point3d Centroid, Use Use)>();
        foreach (var p in model.Parcels)
        {
            var centroid = ComputeCentroid(p.Outline.Geometry);
            if (centroid.HasValue)
                centroids.Add((centroid.Value, p.Use));
        }
        return centroids;
    }

    private static Use ResolveUse(
        Polygon2D hole,
        List<(Point3d Centroid, Use Use)> oldCentroids,
        UrbanModel model)
    {
        if (oldCentroids.Count == 0)
            return model.GetUse("Public");

        var holeCentroid = ComputeCentroid(hole.Geometry);
        if (holeCentroid is null)
            return model.GetUse("Public");

        return oldCentroids
            .OrderBy(x => x.Centroid.DistanceTo(holeCentroid.Value))
            .First().Use;
    }

    private static Point3d? ComputeCentroid(Curve curve)
    {
        var props = AreaMassProperties.Compute(curve);
        return props?.Centroid;
    }
}
