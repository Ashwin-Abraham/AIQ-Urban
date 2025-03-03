using ArchSitePlan.BHoM.Geometry.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry;
public class Rectangle:Polygon
{
    public Point Min { get; }
    public Point Max { get; }
    public Rectangle(Point min, Point max)
        :base(new List<Point> { 
            min, 
            Create.Point(max.X, min.Y), 
            max, 
            Create.Point(min.X, max.Y) }) 
    {
        Min = min;
        Max = max;
    }

    public double AreaAbs()
    {
        return Math.Abs((Max.X - Min.X) * (Max.Y - Min.Y));
    }

    public List<Line> GetLargeSegments()
    {
        return this.ISubParts()
            .OrderByDescending(x => x.ILength())
            .Take(2)
            .Cast<Line>()
            .ToList();
    }

    public List<Line> Contour(int count)
    {
        List<Line> contuors = new();
        double parameterUnit = 1 / (double)count;

        var longSegments = GetLargeSegments();
        var baseCurve = longSegments .Find(line => Min.Distance(line).AlmostZero(0));
        var otherCurve = longSegments.Find(line => line != baseCurve);

        for (double i = 0; i< count; i++)
        {
            double parameter = parameterUnit * i;

            var start = baseCurve.PointAtParameter(parameter);
            var end = otherCurve.PointAtParameter(1 - parameter);

            contuors.Add(Create.Line(start, end));
        }

        return contuors;
    }
}
