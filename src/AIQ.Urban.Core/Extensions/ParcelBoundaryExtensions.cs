using System.Diagnostics.CodeAnalysis;
using AIQ.Primitives;
using AIQ.Primitives.Extensions;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;

public static class ParcelBoundaryExtensions
{
    public static bool TryNormalizeParcelBoundary(
        this Curve boundary,
        [NotNullWhen(true)] out Curve? normalized,
        double tolerance = NumericTolerance.Default)
    {
        normalized = null;
        if (!boundary.IsPlanar())
            return false;

        var bounds = boundary.GetBoundingBox(true);
        if (!bounds.IsValid
            || !bounds.Min.Z.AlmostZero(tolerance)
            || !bounds.Max.Z.AlmostZero(tolerance))
            return false;

        normalized = boundary.DuplicateCurve();
        if (bounds.Min.Z == 0 && bounds.Max.Z == 0)
            return true;

        return normalized.Transform(Transform.PlanarProjection(Plane.WorldXY));
    }
}
