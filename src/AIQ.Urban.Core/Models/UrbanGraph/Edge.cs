using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanGraph;
public class Edge
{
    //TODO: Remove IGraphable or think about refactoring this
    public IGraphableAsEdge? SourceElement { get; } 
    public GraphNode Start { get; } 
    public GraphNode End { get; }

    // TODO: It is odd to have both a direct line connection AND a IGraphable based connection.
    // May need concept simplification
    public Lazy<Curve> Curve { get; }

    public Edge(GraphNode start, GraphNode end, IGraphableAsEdge? source = null)
    {
        if (start == end)
            throw new ArgumentException("Start and end node cannot be the same.");

        SourceElement = source;
        Start  = start;
        End  = end;
        Curve = new Lazy<Curve>(() => CalculateCurve(Start.Point, End.Point, SourceElement.GraphableGeometry));
    }

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
