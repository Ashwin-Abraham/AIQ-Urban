using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanGraph;
public class Graph(List<GraphNode> nodes, List<Edge> edges)
{
    public List<GraphNode> Nodes { get; } = nodes;
    public List<Edge> Edges { get; } = edges;

    public static Graph Create(Profile2D profile, double tolerance = 1e-5)
    {
        var graph = new Graph([], []);

        AddToGraph(graph, profile.Outline, tolerance);

        foreach(var hole in profile.Holes)
            AddToGraph(graph, hole, tolerance);

        return graph;
    }

    public static void AddToGraph(Graph graph, Polycurve2D polygon, double tolerance = 1e-5)
    {
        var points = polygon.Geometry.ToNurbsCurve().Points
            .Select(x => new Point3d(x.X, x.Y, 0))
            .ToList();

        var previousPoint = points[0];

        if (!graph.FindNodeAt(previousPoint, out GraphNode? previousNode, tolerance))
        {
            previousNode = new GraphNode(previousPoint, []);
            graph.Nodes.Add(previousNode);
        }

        for (int i = 1; i < points.Count; i++)
        {
            var thisPoint = points[i];

            if(!graph.FindNodeAt(thisPoint, out GraphNode? thisNode, tolerance))
            {
                thisNode = new GraphNode(thisPoint, []);
                graph.Nodes.Add(thisNode);
            }

            var edgeToPrevious = new Edge(previousNode, thisNode);
            thisNode!.Edges.Add(edgeToPrevious);
            previousNode.Edges.Add(edgeToPrevious);

            graph.Edges.Add(edgeToPrevious);
            previousNode = thisNode;
        }
    }

    private bool FindNodeAt(Point3d thisPoint, out GraphNode? thisNode, double tolerance)
    {
        thisNode = Nodes.FirstOrDefault(x => x.Point.EpsilonEquals(thisPoint, tolerance));
        return thisNode != null;
    }
}