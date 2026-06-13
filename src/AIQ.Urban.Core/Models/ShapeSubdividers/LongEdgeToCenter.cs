using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.ShapeSubdividers;
public class LongEdgeToCenter(int _dividerCount = 2, double _tolerance = 1e-6) : IShapeSubdivider
{
    public DivisionResult Run(Profile2D profile)
    {
        var curve = profile.Outline.Geometry;
        var segments = curve.DuplicateSegments();

        var divisionSegments = segments
            .Select((x, i) => (Index: i, Segment: x))
            .OrderByDescending(x => x.Segment.GetLength())
            .Take(_dividerCount)
            .ToList();

        var divisionRegions = ToRegions(profile.Outline.Geometry, divisionSegments);

        var regions = new List<Polyline>();

        foreach(var region in divisionRegions)
        {
            var overlappedRegions = Curve.CreateBooleanIntersection(profile.Outline.Geometry, region, _tolerance);

            var overlappedPlines = overlappedRegions
                .SelectMany(x => CurveToPolylines(x, _tolerance))
                .ToList();
            
            regions.AddRange(overlappedPlines);
        }

        var remainderPolylines = Curve.CreateBooleanDifference(profile.Outline.Geometry, divisionRegions, _tolerance)
            .SelectMany(x => CurveToPolylines(x, _tolerance))
            .ToList();

        regions.AddRange(remainderPolylines);
        
        var dividers = divisionSegments.Select(x => new Curve2D(x.Segment, Guid.NewGuid())).ToList();
        var fragments = regions
            .Select(x =>
            {
                var pline = x.ToPolylineCurve();
                var polygon = new Polygon2D(pline);
                var brep = Brep.CreatePlanarBreps(pline, _tolerance)[0];
                return new Profile2D(brep, polygon, []);
            })
            .ToList();

        return new DivisionResult(fragments, dividers);
    }

    private List<PolylineCurve> ToRegions(PolylineCurve outline, List<(int Index, Curve Segment)> divisionSegments)
    {
        var centroid = AreaMassProperties.Compute(outline).Centroid;

        // Assuming CW winding
        List<PolylineCurve> regions = [];

        for(int i = 0;  i < divisionSegments.Count; i++)
        {
            var thisOne = divisionSegments[i];
            var nextOne = divisionSegments[(i + 1) % divisionSegments.Count];

            var firstPoint = thisOne.Segment.PointAtMid;
            var interveningPoints = GetInterveningPoints(outline, thisOne.Index, nextOne.Index).ToList();
            var lastPoint = nextOne.Segment.PointAtMid;
            var points = interveningPoints
                .Prepend(firstPoint)
                .Append(lastPoint)
                .Append(centroid)
                .Append(firstPoint);

            var region = new PolylineCurve(points);
            regions.Add(region);
        }

        return regions;
    }

    private static List<Polyline> CurveToPolylines(Curve curve, double tolerance)
    {
        if (curve.TryGetPolyline(out var pline))
            return [pline];

        var segments = curve.DuplicateSegments();
        if (segments is null || segments.Length == 0)
            return [];

        var polyline = new Polyline { curve.PointAtStart };
        foreach (var seg in segments)
            polyline.Add(seg.PointAtEnd);

        if (polyline.Count < 2)
            return [];

        var last = polyline.Count - 1;
        if (last > 0 && polyline[last].DistanceTo(polyline[0]) < tolerance)
            polyline.RemoveAt(last);

        return [polyline];
    }

    private IEnumerable<Point3d> GetInterveningPoints(PolylineCurve curve, int thisSegmentIndex, int nextSegmentIndex)
    {
        var count = curve.PointCount - 1;

        thisSegmentIndex = ((thisSegmentIndex % count) + count) % count;
        nextSegmentIndex = ((nextSegmentIndex % count) + count) % count;

        if (thisSegmentIndex == nextSegmentIndex)
            yield break;

        var i = (thisSegmentIndex + 1) % count;
        while (i != nextSegmentIndex)
        {
            yield return curve.Point(i);
            i = (i + 1) % count;
        }
    }
}
