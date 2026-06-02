using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanGraph;
public class Edge(GraphNode start, GraphNode end, IGraphableAsEdge? source = null)
{
    public IGraphableAsEdge? SourceElement { get; } = source;
    public GraphNode Start { get; } = start;
    public GraphNode End { get; } = end;

    public Lazy<Curve> Curve { get; } = new Lazy<Curve>(() => CalculateCurve(start.Point, end.Point, source.GraphableGeometry));

    private static Curve CalculateCurve(Point3d start, Point3d end, Curve parent)
    {
        parent.ClosestPoint(start, out double startParam);
        parent.ClosestPoint(end, out double endParam);

        var fragments = parent.Split([startParam, endParam]);
        
        foreach(var curve in fragments)
        {
            var mid = curve.PointAtMid;
            parent.ClosestPoint(mid, out double midParam);

            if(midParam > startParam && midParam < endParam)
                return curve;
        }

        return fragments[0];
    }
}
