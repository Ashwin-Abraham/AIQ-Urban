using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;

public static class PlanarSurfaceExtensions
{
    public static Point ControlPointCentroid(this PlanarSurface surface)
    => surface.ExternalBoundary.ControlPointCentroid();

    public static List<PlanarSurface> Split(
        this PlanarSurface surface,
        IEnumerable<ICurve> splitters,
        double tolerance = 1E-06)
    {
        var externalBoundaries = surface
            .ExternalBoundary
            .ToBoundaryCurve()!
            .SplitIntoClosedCurves2(splitters.ToList(), tolerance);
        
        var internalBoundaries = surface.InternalBoundaries
            .Select(crv => crv.ToBoundaryCurve()!)
            .SelectMany(c => c.SplitIntoClosedCurves2(splitters.ToList(), tolerance))
            .Cast<ICurve>()
            .ToList();

        var splitSrfs = new List<PlanarSurface>();

        foreach (var curve in externalBoundaries)
        {
            var fragment = Create.PlanarSurface(curve, internalBoundaries);

            if (fragment is not null)
                splitSrfs.Add(fragment);
        }
        return splitSrfs;
    }
}
