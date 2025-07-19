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

    public static List<PlanarSurface> BooleanUnion (this List<PlanarSurface> surfaces, double tolerance = 1e-06)
    {
        var surfaceGroups = surfaces.GroupClashingSurfaces(tolerance);
        var result = new List<PlanarSurface>();

        foreach(var group in surfaceGroups)
        {
            result.AddRange(BooleanUnionGrouped(group, tolerance));
        }

        return result;
    }

    public static bool IsContainingAll(this PlanarSurface surface, List<Point> points)
    {
        var allPointsWithinExternal = points.All(p => surface.ExternalBoundary.IIsContaining([p], false));
        if(!allPointsWithinExternal) return false;

        foreach(var hole  in surface.InternalBoundaries)
        {
            var anyPointsWithinHole = points.Any(p => hole.IIsContaining([p], false));
            if (anyPointsWithinHole) return false;
        }

        return true;
    }


    public static List<List<PlanarSurface>> GroupClashingSurfaces (this List<PlanarSurface> surfaces, double tolerance = 1e-06)
    {
        if (surfaces.Count == 0) return [];

        List<List<PlanarSurface>> surfaceGroups = new() { new() { surfaces[0] } };

        for(int i = 1; i < surfaces.Count; i++)
        {
            var testSurface = surfaces[i];
            List<int> groupsItClashesWith = new();

            for(int groupNo = 0; groupNo< surfaceGroups.Count; groupNo++)
            {
                bool isPartOfGroup = surfaceGroups[groupNo].Any(x =>
                testSurface.ExternalBoundary.ICurveIntersections(x.ExternalBoundary).Count > 0
                || testSurface.ExternalBoundary.ICurveIntersections(x.InternalBoundaries).Count > 0
                || x.IsContainingAll(testSurface.ExternalBoundary.IControlPoints()));

                if (!isPartOfGroup) continue;

                groupsItClashesWith.Add(groupNo);
            }

            if(groupsItClashesWith.Count == 0)
            {
                surfaceGroups.Add(new() { testSurface });
            }
            // If the new surface clashes with 1 group, add to just that group
            else if (groupsItClashesWith.Count == 1)
            {
                surfaceGroups[groupsItClashesWith[0]].Add(testSurface);
            }
            // If the new surface clashes with multiple groups, merge the groups
            else
            {
                var mergedGroup = new List<PlanarSurface>() { testSurface };
                var nonClashingGroups = surfaceGroups.Where((x, i) => !groupsItClashesWith.Contains(i));
                var clashingGroups = surfaceGroups.Where((x, i) => groupsItClashesWith.Contains(i)).ToList();
                clashingGroups.ForEach(x => mergedGroup.AddRange(x));
                surfaceGroups = nonClashingGroups.Append(mergedGroup).ToList();
            }
        }

        return surfaceGroups;
    }

    private static List<PlanarSurface> BooleanUnionGrouped (List<PlanarSurface> surfaces, double tolerance = 1e-06)
    {
        var externals = surfaces.Select(x => x.ExternalBoundary).ToList();

        var externalsUnioned = Compute.BooleanUnion(externals);

        // Bring back swallowed up externals, ie 

        var holes = new List<ICurve>();


        // When a hole is generated as a result of a union, this needs to be extracted
        for(int i = 0; i< externalsUnioned.Count; i++)
        {
            var crv = externalsUnioned[i];
            var testCurves = externalsUnioned.Where(x => x != crv).ToList();
            bool containedByAny = testCurves.Exists(x => x.IIsContaining(crv));

            if (!containedByAny) continue;

            externalsUnioned.RemoveAt(i);
            i--;
            holes.Add(crv);
        }

        for(int i = 0; i< surfaces.Count; i++)
        {
            var surface = surfaces[i];
            var otherSurfaces = surfaces.Except([surface]);
            
            var thisSrfHoles = surface.InternalBoundaries;
            var thisSrfDiffedHoles = thisSrfHoles.SelectMany(x => x.BooleanDifference(otherSurfaces.ToList()));

            holes.AddRange(thisSrfDiffedHoles);
        }

        holes = holes.BooleanUnion().Cast<ICurve>().ToList();
        return externalsUnioned.Select(x => new PlanarSurface(x, holes)).ToList();
    }

    public static List<PlanarSurface> BooleanDifference(this PlanarSurface surface, List<PlanarSurface> secondSet, double tolerance = 1e-06)
    {
        var secondSetUnioned = secondSet.BooleanUnion(tolerance);

        var diffExternals = secondSetUnioned.Select(x => x.ExternalBoundary).ToList();
        var diffHolesUnioned = secondSetUnioned.SelectMany(x => x.InternalBoundaries).BooleanUnion();

        var zoneOutsideExternalDiffs = surface.ExternalBoundary.BooleanDifference(diffExternals);
        var externalsDiffed = new List<ICurve>();
        var holes = new List<ICurve>();
        holes.AddRange(surface.InternalBoundaries);

        // Remove holes that have appeared in the externals
        foreach(var external in zoneOutsideExternalDiffs)
        {
            var others = zoneOutsideExternalDiffs.Except([external]);
            if (!others.Any(x => x.IIsContaining(external)))
                externalsDiffed.Add(external);
            else
                holes.Add(external);
        }

        var zoneWithinHolesOfDiff = diffHolesUnioned.SelectMany(x => x.BooleanIntersection(surface.ExternalBoundary)).ToList();

        var externals = externalsDiffed.Concat(zoneWithinHolesOfDiff).ToList();

        var remainingSurfaces = externals.Select(x => Create.PlanarSurface(x, holes)).ToList();
        return remainingSurfaces;
    }

    public static List<PlanarSurface> BooleanDifference(this PlanarSurface surface, List<ICurve> subtractors, double tolerance = 1e-06)
    {
        if (subtractors.Any(x => x.IIsContaining(surface.ExternalBoundary))) return [];

        var subtracted = surface.ExternalBoundary.BooleanDifference(subtractors, tolerance).Cast<ICurve>();

        SeparateExternalsAndContainedCurves(
            subtracted.ToList(),
            out var subtracted_nonHoles,
            out var additionalHoles);

        var holes = surface.InternalBoundaries.Concat(additionalHoles).ToList();
        return subtracted_nonHoles.Select(x => Create.PlanarSurface(x, holes)).ToList();
    }

    private static void SeparateExternalsAndContainedCurves(
        List<ICurve> curves,
        out List<ICurve> externals,
        out List<ICurve> internals)
    {
        internals = new List<ICurve>();
        externals = new List<ICurve>();

        foreach (var crv in curves)
        {
            var otherCrvs = curves.Except([crv]);
            if (otherCrvs.Any(x => x.IIsContaining(crv)))
                internals.Add(crv);
            else
                externals.Add(crv);
        }
    }

    public static List<PlanarSurface> BooleanIntersection(this PlanarSurface surface, List<PlanarSurface> intersectors, double tolerance = 1e-06)
    {
        var externals = intersectors
            .SelectMany(x => x.ExternalBoundary.BooleanIntersection(surface.ExternalBoundary))
            .Select(x => Create.PlanarSurface(x))
            .ToList();
        
        var internals = intersectors.SelectMany(x => x.InternalBoundaries)
            .Concat(surface.InternalBoundaries)
            .ToList();

        var result = new List<PlanarSurface>();

        foreach(var external in externals)
        {
            var remaining = external.BooleanDifference(internals);
            result.AddRange(remaining);
        }

        return result;
    }
}
