using AIQ.Primitives.Extensions;
using ArchSitePlan.BHoM.Geometry.Comparers;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;

public static class PolygonExtensions
{
    public static List<double> GetPolygonInternalAngles(this Polygon polygon)
    {
        var segments = polygon.SubParts().ToList();
        List<double> anglesOfSide1 = [];
        List<double> interiorAngles = [];

        for (int i = 0; i < segments.Count; i++)
        {
            var direction1 = segments[i].Direction();
            var direction2 = segments[(i + 1) % segments.Count].Direction();

            var angle = direction1.SignedAngle(direction2, Vector.ZAxis).RadiansToDegrees();

            var interiorAngle = 180 - angle;

            interiorAngles.Add(interiorAngle);
        }

        var sumOfAngles = polygon.SumOfInternalAngles();

        if(interiorAngles.Sum().AlmostEquals(sumOfAngles))
            return interiorAngles;

        return interiorAngles.Select(x => 360 - x).ToList();
    }

    public static IEnumerable<Line> SubParts(this Polygon polygon)
    {
        return polygon.ISubParts().Cast<Line>();
    }

    public static double SumOfInternalAngles(this Polygon polygon)
    {
        if (polygon.Vertices.Count < 3)
            return 0.0;
        
        return (polygon.Vertices.Count - 2) * 180.0;
    }

    public static Polyline ToClosedPolyline(this Polygon polygon)
    {
        var vertices = polygon.Vertices.ToList();
        vertices.Add(polygon.Vertices[0]);

        return Create.Polyline(vertices);
    }

    public static Polygon Transform(this Polygon polygon, TransformMatrix transform)
    {
        var vertices = polygon.Vertices.Select(x => x.Transform(transform)).ToList();
        return new Polygon(vertices);
    }

    /// <summary>
    /// Inserts a vertex into the polygon at the correct position based on collinearity
    /// with a segment. If not found, returns the original polygon.
    /// </summary>
    public static Polygon InsertVertices(this Polygon polygon, IEnumerable<Point> points)
    {
       // Bug is here. Debug it. When 2 points are sent in, second one isnt being added
        var vertexList = polygon.Vertices.ToList();

        var insertionList = points.Where(x => !vertexList.Contains(x, PointComparer.Instance)).ToList();

        var colinearPoints = insertionList
            .Where(x => x.Distance(polygon).AlmostZero())
            .ToList();

        var newPoints = colinearPoints.Concat(vertexList)
            .Distinct(PointComparer.Instance)
            .OrderBy(x => polygon.IParameterAtPoint(x))
            .ToList();

        var newPolygon = Create.Polygon(newPoints);
        return newPolygon;
    }

    public static int ClosestSegmentIndex(this Polygon polygon, Point point)
    {
        var segments = polygon.ISubParts().Cast<Line>();

        var closestIndex = segments
            .Select((segment, index) => new { Segment = segment, Index = index })
            .MinBy(s => point.Distance(s.Segment))
            .Index;

        return closestIndex;
    }

    public static Vector GetOutwardPerpendicularAtSegment(this Polygon polygon, int segmentIndex)
    {
        var segment = polygon.SubParts().ElementAtOrDefault(segmentIndex);
        var normal = polygon.Normal();

        if (segment == null)
            return null;

        return segment.Direction().CrossProduct(normal).Unitized();
    }

    public static bool TryOffsetSegment(this Polygon polygon, double amount, int index, out Polygon? result)
    {
        result = polygon;

        if (index < 0 || index > polygon.Vertices.Count)
            return false;

        var oldVertices = polygon.Vertices.ToList();

        var zerothPoint = oldVertices[(index - 1 + polygon.Vertices.Count) % polygon.Vertices.Count];
        var firstPoint = oldVertices[index];
        var secondPoint = oldVertices[(index + 1)% polygon.Vertices.Count];
        var thirdPoint = oldVertices[(index + 2) % polygon.Vertices.Count];

        var outDirection = polygon.GetOutwardPerpendicularAtSegment(index);

        var offsetFirst = firstPoint + outDirection * amount;
        var offsetSecond = secondPoint + outDirection * amount;

        var newVertices = oldVertices.Take(index).ToList();

        if (new List<Point>() { zerothPoint, firstPoint, secondPoint }.AreColinear2D())
            newVertices.Add(firstPoint);

        newVertices.Add(offsetFirst);
        newVertices.Add(offsetSecond);

        if (new List<Point>() { firstPoint, secondPoint, thirdPoint }.AreColinear2D())
            newVertices.Add(secondPoint);

        newVertices.AddRange(oldVertices.Skip(index + 2));

        var newPolygon = Create.Polygon(newVertices.ToList());

        if (newPolygon == null)
            return false;

        result = newPolygon.RemovedColinear();

        return result?.IsSelfIntersecting()?? false;
    }

    public static Polygon RemovedColinear(this Polygon polygon)
    {
        var vertices = polygon.Vertices;

        var newVertices = new List<Point>();

        for(int i = 0; i< vertices.Count; i++)
        {
            var a = vertices[(i - 1 + vertices.Count) % vertices.Count];
            var b = vertices[i];
            var c = vertices[(i + 1) % vertices.Count];

            bool pointsColinear = new List<Point>() { a, b, c }.AreColinear2D();

            if(!pointsColinear)
                newVertices.Add(b);
        }

        return new Polygon(newVertices.ToList());
    }
}
