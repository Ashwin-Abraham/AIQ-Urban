using AIQ.Primitives;
using AIQ.Primitives.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;
using System.Diagnostics.CodeAnalysis;

namespace AIQ.Urban.Core.Extensions;

public static class TransitExtensions
{
    public static bool IsOnTransitGroundPlane(
        this Curve centerline,
        double tolerance = NumericTolerance.Default)
    {
        ArgumentNullException.ThrowIfNull(centerline);

        var bounds = centerline.GetBoundingBox(true);
        return bounds.IsValid
            && bounds.Min.Z.AlmostZero(tolerance)
            && bounds.Max.Z.AlmostZero(tolerance);
    }

    public static bool TryNormalizeTransitCenterline(
        this Curve centerline,
        [NotNullWhen(true)] out Curve? normalized,
        double tolerance = NumericTolerance.Default)
    {
        normalized = null;
        if (!centerline.IsOnTransitGroundPlane(tolerance))
            return false;

        normalized = centerline.DuplicateCurve();
        var bounds = normalized.GetBoundingBox(true);
        if (bounds.Min.Z == 0 && bounds.Max.Z == 0)
            return true;

        return normalized.Transform(Transform.PlanarProjection(Plane.WorldXY));
    }

    public static Curve NormalizeTransitCenterline(
        this Curve centerline,
        double tolerance = NumericTolerance.Default)
    {
        if (!centerline.TryNormalizeTransitCenterline(out var normalized, tolerance))
            throw new ArgumentException(
                $"Transit centerline must lie at Z = 0 within tolerance {tolerance}.",
                nameof(centerline));

        return normalized!;
    }

    public static Curve2D NormalizeTransitCenterline(
        this Curve2D centerline,
        double tolerance = NumericTolerance.Default)
    {
        ArgumentNullException.ThrowIfNull(centerline);
        return new Curve2D(
            centerline.Geometry.NormalizeTransitCenterline(tolerance),
            centerline.Id);
    }
}
