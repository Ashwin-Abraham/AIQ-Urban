using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.ShapeSubdividers;

public class BisectingCrossSection(double _tolerance = 1e-6) : IShapeSubdivider
{
    public DivisionResult Run(Profile2D profile)
    {
        var outline = profile.Outline.Geometry;

        var canGetPolyline = outline.TryGetPolyline(out var polyline);
        System.Diagnostics.Debug.WriteLine(
            $"BisectingCrossSection: TryGetPolyline={canGetPolyline}, Count={polyline?.Count}, Holes={profile.Holes.Count}");

        if (!canGetPolyline)
            return new DivisionResult([profile], []);

        if (polyline.Count < 3)
            return new DivisionResult([profile], []);

        var box = polyline.CreateMinimalBox();
        System.Diagnostics.Debug.WriteLine(
            $"BisectingCrossSection: box is null = {box is null}");

        if (box is null)
            return new DivisionResult([profile], []);

        var boxPts = box.ToArray();
        var longestIdx = FindLongestEdge(boxPts);

        var scanlineStart = (boxPts[longestIdx] + boxPts[(longestIdx + 1) % 4]) / 2.0;
        var scanlineEnd = (boxPts[(longestIdx + 2) % 4] + boxPts[(longestIdx + 3) % 4]) / 2.0;

        var halfRegions = new[]
        {
            new PolylineCurve([
                scanlineStart,
                boxPts[(longestIdx + 1) % 4],
                boxPts[(longestIdx + 2) % 4],
                scanlineEnd,
                scanlineStart
            ]),
            new PolylineCurve([
                scanlineEnd,
                boxPts[(longestIdx + 3) % 4],
                boxPts[longestIdx],
                scanlineStart,
                scanlineEnd
            ])
        };

        var fragments = new List<Profile2D>();

        foreach (var region in halfRegions)
        {
            var outlineFrags = Curve.CreateBooleanIntersection(outline, region, _tolerance);
            if (outlineFrags.Length == 0)
                continue;

            foreach (var outlineFrag in outlineFrags)
            {
                var fragmentProfile = BuildProfile(outlineFrag, profile.Holes, region);
                if (fragmentProfile is not null)
                    fragments.Add(fragmentProfile);
            }
        }

        var scanlineCurve = new LineCurve(scanlineStart, scanlineEnd);
        var divider = new Curve2D(scanlineCurve, Guid.NewGuid());

        return new DivisionResult(fragments, [divider]);
    }

    private Profile2D? BuildProfile(Curve outlineFrag, List<Polygon2D> originalHoles, PolylineCurve region)
    {
        var outlinePline = CurveToPolyline(outlineFrag, _tolerance);
        if (outlinePline.Count < 3)
            return null;

        var outlineCurve = new PolylineCurve(outlinePline);
        var polygon = new Polygon2D(outlineCurve);

        var holeCurves = new List<Curve>();
        foreach (var hole in originalHoles)
        {
            var holePline = CurveToPolyline(hole.Geometry, _tolerance);
            if (holePline.Count < 3)
                continue;

            var ccwHolePline = EnsureWinding(holePline, clockwise: false);
            var ccwHoleCurve = new PolylineCurve(ccwHolePline);

            var holeFrags = Curve.CreateBooleanIntersection(ccwHoleCurve, region, _tolerance);
            foreach (var frag in holeFrags)
            {
                var fragPline = CurveToPolyline(frag, _tolerance);
                if (fragPline.Count < 3)
                    continue;

                var cwFragPline = EnsureWinding(fragPline, clockwise: true);
                holeCurves.Add(new PolylineCurve(cwFragPline));
            }
        }

        var curvesForBrep = new Curve[1 + holeCurves.Count];
        curvesForBrep[0] = outlineCurve;
        for (var i = 0; i < holeCurves.Count; i++)
            curvesForBrep[1 + i] = holeCurves[i];

        var brep = Brep.CreatePlanarBreps(curvesForBrep, _tolerance);
        if (brep.Length != 1)
            return null;

        var holePolygons = new List<Polygon2D>();
        foreach (var hc in holeCurves)
            holePolygons.Add(new Polygon2D(hc));

        return new Profile2D(brep[0], polygon, holePolygons);
    }

    private static Polyline EnsureWinding(Polyline pline, bool clockwise)
    {
        if (pline.Count < 3)
            return pline;

        var signedArea = 0.0;
        for (var i = 0; i < pline.Count - 1; i++)
            signedArea += pline[i].X * pline[i + 1].Y - pline[i + 1].X * pline[i].Y;

        var isCCW = signedArea > 0;
        if ((clockwise && isCCW) || (!clockwise && !isCCW))
        {
            var reversed = new Polyline();
            for (var i = pline.Count - 1; i >= 0; i--)
                reversed.Add(pline[i]);
            return reversed;
        }

        return pline;
    }

    private static Polyline CurveToPolyline(Curve curve, double tolerance)
    {
        if (curve.TryGetPolyline(out var pline))
        {
            if (pline.Count >= 2 && pline[0].DistanceTo(pline[^1]) > tolerance)
                pline.Add(pline[0]);
            return pline;
        }

        var segments = curve.DuplicateSegments();
        if (segments is null || segments.Length == 0)
            return new Polyline { curve.PointAtStart, curve.PointAtEnd };

        var polyline = new Polyline { curve.PointAtStart };
        foreach (var seg in segments)
            polyline.Add(seg.PointAtEnd);

        if (polyline.Count >= 3 && polyline[0].DistanceTo(polyline[^1]) > tolerance)
            polyline.Add(polyline[0]);

        return polyline;
    }

    private static int FindLongestEdge(Point3d[] boxPts)
    {
        var longestIdx = 0;
        var maxLen = 0.0;

        for (var i = 0; i < 4; i++)
        {
            var len = boxPts[i].DistanceTo(boxPts[(i + 1) % 4]);
            if (len > maxLen)
            {
                maxLen = len;
                longestIdx = i;
            }
        }

        return longestIdx;
    }
}
