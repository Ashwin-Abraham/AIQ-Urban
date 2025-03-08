using AIQ.Primitives.Extensions;
using ArchSitePlan.BHoM.Geometry.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Models;
public class Rectangle:Polygon
{
    public Point Min { get; }
    public Point Max { get; }
    public Rectangle(Point a, Point b, Point c, Point d)
        : base(new List<Point>() { a, b, c, d }) 
    {
        Min = a;
        Max = c;
    }

    public Rectangle(Point min, Point max)
        :this(
            min, 
            Create.Point(max.X, min.Y), 
            max, 
            Create.Point(min.X, max.Y)) 
    { }

    public double AreaAbs()
    {
        return Math.Abs((Max.X - Min.X) * (Max.Y - Min.Y));
    }

    public List<Line> GetLargeSegments()
    {
        var orderedParts = this.ISubParts()
            .OrderByDescending(x => x.ILength())
            .ToList();

        var longestLength = orderedParts[0].ILength();

        var largeSides = orderedParts
            .Where(part => part.ILength().AlmostEquals(longestLength))
            .Cast<Line>()
            .ToList();

        return largeSides;
    }

    public Rectangle Rotate(Point point, Vector axis, double rad)
    {
        var pline = this.IRotate(point, axis, rad);
        var points = pline.IControlPoints();
        return new Rectangle(points[0], points[1], points[2], points[3]);
    }

    public Line LongestSegment()
        => (this.ISubParts().MaxBy(x => x.ILength()) as Line)!;

    public Line ShortestSegment()
        => (this.ISubParts().MinBy(x => x.ILength()) as Line)!;

    public List<Line> Contour(int count, bool cutAlongLength = true)
    {
        var longestSegment = LongestSegment();

        var nextSegment = this.ISubParts()
            .Cast<Line>()
            .ToList()
            .Find(x => x.Start.AlmostEquals(longestSegment.End));
        
        var moverAlongContour = nextSegment
            .Direction()
            .Unitized()
            * ShortestSegment().Length();

        var firstContour = Create.Line(
            longestSegment.Start,
            longestSegment.Start.Translate(moverAlongContour));

        if (count == 1)  return [firstContour];

        var moverAlongAxis = longestSegment
            .Direction()
            .Unitized()
            * (longestSegment.Length() / (count - 1));

        List<Line> contours = new();

        for(int i = 0; i< count; i++)
            contours.Add(firstContour.Translate(moverAlongAxis * i));

        return contours;
    }
}
