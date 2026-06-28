using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanGraph;
public class GraphNode
{
    public List<Edge> Edges { get; }
    public Point3d Point { get; }

    public IGraphableAsNode? SourceObject { get; }

    public GraphNode(Point3d point, List<Edge> edges)
    {
        Point = point;
        Edges = edges;
        SourceObject = null;
    }

    public GraphNode(IGraphableAsNode sourceObject, List<Edge> edges)
    {
        SourceObject = sourceObject;
        Point = sourceObject.NodePoint;
        Edges = edges;
    }

    public HashSet<GraphNode> Neighbours()
    {
        HashSet<GraphNode> neighbours = [];
        for(int i = 0; i < Edges.Count; i++)
        {
            neighbours.Add(Edges[i].Start);
            neighbours.Add(Edges[i].End);
        }

        neighbours.Remove(this);

        return neighbours;
    }
}
