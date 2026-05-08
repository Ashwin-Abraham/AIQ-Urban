using AIQ.Primitives.Extensions;
using ArchSitePlan.BHoM.Geometry.Models;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;

public static class LineExtensions
{
    public static bool OverlapsOfMinimumSize(this Line line, IEnumerable<Line> lines, double minimumOverlap)
    {
        foreach (var x in lines)
        {
            var overlap = line.Overlaps(x);
            if (overlap != null && overlap.Length().GreaterThanORAlmostEquals(minimumOverlap))
                return true;
        }

        return false;
    }

    public static List<Line> Overlaps(this Line line, IEnumerable<Line> lines)
    {
        return lines.Select(x => line.Overlaps(x))
            .Where(x => x != null)
            .Cast<Line>()
            .ToList();
    }

    public static Line? Overlaps(this Line line, Line otherLine)
    {
        var intersections = line.LineIntersections(otherLine);
        
        Line overlap = null;
        if (intersections.Count == 2 && 
            !intersections[0].Distance(intersections[1]).AlmostZero())
            overlap = Create.Line(intersections[0], intersections[1]);

        return overlap;
    }

    public static List<Point> DivideByDimension(this Line line, double size, bool includeEnds = false)
    {
        var length = line.Length();
        var points = new List<Point>();

        if(includeEnds)
        {
            points.Add(line.Start);
            points.Add(line.End);
        }

        if (size.GreaterThanORAlmostEquals(length) || size.LessThanORAlmostEquals(0))
            return points;

        double position = size;

        while (!position.GreaterThanORAlmostEquals(length))
        {
            points.Add(line.PointAtLength(position));
            position += size;
        }

        return points;
    }

    public static Point Mid(this Line line)
    {
        return (line.Start + line.End)/ 2;
    }

    /// <summary>
    /// Works only in XY plane
    /// Generates an arc between Line a End and Line b start
    /// </summary>
    public static PolyCurve ArcBetweenLinesThatMeet(
        this Line a,
        Line b,
        double radius,
        double tolerance = 1E-06)
    {
        double angleBetweenLines = a.SignedAngle(b, Vector.ZAxis);
        double trim = Math.Abs(Math.Tan(angleBetweenLines / 2) * radius);
        double distanceOfArcCenterFromIntersect = radius / (Math.Cos(angleBetweenLines / 2));

        if (a.Length() < trim
            || b.Length() < trim
            || a.End.Distance(b.Start) > tolerance)
            return Create.PolyCurve(new List<Line>() { a, b });

        var bisectorDirection = (a.Direction().Reverse() + b.Direction()) / 2;
        var arcCenter = a.End + bisectorDirection * distanceOfArcCenterFromIntersect;
        var arcMid = arcCenter + bisectorDirection.Reverse() * radius;

        Line shortA = a.Extend(0, -trim);
        Line shortB = b.Extend(-trim, 0);
        Arc arc = Create.Arc(shortA.End, arcMid, shortB.Start);

        PolyCurve polyCurve = new PolyCurve();
        polyCurve.Curves.Add(shortA);
        polyCurve.Curves.Add(arc);
        polyCurve.Curves.Add(shortB);
        return polyCurve;
    }
}
