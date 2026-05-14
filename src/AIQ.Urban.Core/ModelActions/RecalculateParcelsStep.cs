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
/// polygon centroids. Falls back to <see cref="DefaultUses.Public"/> when no
/// old parcels exist (e.g. first run).
///
/// NOTE: If multiple transit types produce overlapping holes in the same
/// physical area, duplicate parcels will be created. No dedup is performed.
/// </summary>
public class RecalculateParcelsStep : IDeleteUrbanElementsStep, IAddUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model)
    {
        var oldParcelIds = model.Parcels.SelectMany(p => p.GetGeometryIds()).ToList();

        var oldCentroids = new List<(Point3d Centroid, Use Use)>();
        foreach (var p in model.Parcels)
        {
            var centroid = ComputeCentroid(p.Outline.Geometry);
            if (centroid.HasValue)
                oldCentroids.Add((centroid.Value, p.Use));
        }

        model.Parcels.Clear();

        foreach (var (profile, _) in model.TransitSurfaces)
        {
            foreach (var hole in profile.Holes)
            {
                var use = ResolveUse(hole, oldCentroids);
                model.Parcels.Add(new Parcel(hole, use));
            }
        }

        var newParcelIds = model.Parcels.SelectMany(p => p.GetGeometryIds()).ToList();

        return oldParcelIds.Concat(newParcelIds).ToList();
    }

    private static Use ResolveUse(
        Polygon2D hole,
        List<(Point3d Centroid, Use Use)> oldCentroids)
    {
        if (oldCentroids.Count == 0)
            return DefaultUses.Public;

        var holeCentroid = ComputeCentroid(hole.Geometry);
        if (holeCentroid is null)
            return DefaultUses.Public;

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
