using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

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
public class RecalculateParcelsStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<RecalculateParcelsStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var oldParcelIds = model.Parcels.SelectMany(p => p.GetGeometryIds()).ToList();

        var externalCentroids = creationData.OfType<CentroidUseData>()
            .Select(c => (c.Centroid, c.Use))
            .ToList();

        var oldCentroids = externalCentroids.Count > 0
            ? externalCentroids
            : CaptureParcelCentroids(model);

        model.Parcels.Clear();
        var splitters = model.ParcelBoundaries.Select(x => x.Curve).ToList();
        var openSplitters = splitters.Where(x => !x.Geometry.IsClosed).ToList();
        var closedSplitters = splitters.Where(x => x.Geometry.IsClosed).ToList();

        foreach (var (profile, _) in model.TransitSurfaces)
        {
            foreach (var hole in profile.Holes)
            {
                var splitOutlines = hole.Split(openSplitters);
                splitOutlines.AddRange(closedSplitters
                    .Where(x => IsEnclosedBy(x.Geometry, hole.Geometry))
                    .Select(x => new PolyCurve2D(x.Geometry)));
                var originalCentroid = hole.Centroid();

                foreach (var parcelOutline in splitOutlines)
                {
                    var use = ResolveUse(parcelOutline, oldCentroids, originalCentroid, model);
                    model.Parcels.Add(new Parcel(parcelOutline, use));
                }
            }
        }

        _logger.LogInformation("Recalculated {Count} parcels", model.Parcels.Count);

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
        PolyCurve2D hole,
        List<(Point3d Centroid, Use Use)> oldCentroids,
        Point3d? originalHoleCentroid,
        UrbanModel model)
    {
        if (oldCentroids.Count == 0)
            return model.GetUse("Public");

        if (originalHoleCentroid.HasValue)
        {
            var matchByOrigin = oldCentroids
                .OrderBy(x => x.Centroid.DistanceTo(originalHoleCentroid.Value))
                .First();
            if (matchByOrigin.Centroid.DistanceTo(originalHoleCentroid.Value) < 1e-6)
                return matchByOrigin.Use;
        }

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

    private static bool IsEnclosedBy(Curve boundary, Curve container, double tolerance = 1e-6)
    {
        if (Intersection.CurveCurve(boundary, container, tolerance, tolerance)?.Count > 0)
            return false;

        var centroid = ComputeCentroid(boundary);
        if (centroid is null)
            return false;

        return IsInsideOrCoincident(container, boundary.PointAtStart, tolerance)
            && IsInsideOrCoincident(container, centroid.Value, tolerance);
    }

    private static bool IsInsideOrCoincident(Curve container, Point3d point, double tolerance)
    {
        var containment = container.Contains(point, Plane.WorldXY, tolerance);
        return containment is PointContainment.Inside or PointContainment.Coincident;
    }

}
