using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.PolygonSubdivider;
public class LongEdgeToCenter(int _dividerCount = 2, double _tolerance = 1e-6) : IShapeSubdivider
{
    public DivisionResult Run(Profile2D profile)
    {
        var centroid = profile.Centroid();
        var centroid3d = new Point3d(centroid.X, centroid.Y, 0);

        var curve = profile.Outline.Geometry;
        var segments = curve.DuplicateSegments();

        var divisionSegments = segments
            .Select((x, i) => (Index: i, Segment: x))
            .OrderByDescending(x => x.Segment.GetLength())
            .Take(_dividerCount)
            .ToList();

        var divisionRegions = ToRegions(profile.Outline.Geometry, divisionSegments);

        var regions = new List<PolylineCurve>();

        foreach(var region in divisionRegions)
        {
            var overlappedRegions = Curve.CreateBooleanIntersection(profile.Outline.Geometry, region, _tolerance);

            var overlappedPlines = overlappedRegions
                .Select(x => {
                    return x.TryGetPolyline(out var pline) ?
                     pline : null;
                })
                .OfType<Polyline>()
                .Select(x => new PolylineCurve(x))
                .ToList();
            
            regions.AddRange(overlappedPlines);
        }

        Polyline? polyline = null;
        bool validRemains = Curve.CreateBooleanDifference(profile.Outline.Geometry, divisionRegions, _tolerance)
            .FirstOrDefault()?
            .TryGetPolyline(out polyline)?? false;

        if(validRemains)
            regions.Add(polyline!.ToPolylineCurve());
        
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
                .Append(centroid);

            var region = new PolylineCurve(points);
            regions.Add(region);
        }

        return regions;
    }

    private IEnumerable<Point3d> GetInterveningPoints(PolylineCurve curve, int thisSegmentIndex, int nextSegmentIndex)
    {
        if (thisSegmentIndex < 0 || nextSegmentIndex < 0 || thisSegmentIndex == nextSegmentIndex)
            yield break;

        thisSegmentIndex = thisSegmentIndex % (curve.PointCount - 1);
        nextSegmentIndex = nextSegmentIndex % (curve.PointCount - 1);

        for(int i = thisSegmentIndex + 1; i != nextSegmentIndex; i++)
        {
            // Ensure wrap around
            i = i % (curve.PointCount - 1);

            yield return curve.Point(i);
        }
    }
}
