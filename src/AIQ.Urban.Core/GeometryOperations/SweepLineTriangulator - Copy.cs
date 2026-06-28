using AIQ.Primitives.Extensions;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanGraph;
using Rhino.Geometry;

namespace AIQ.Urban.Core.GeometryOperations;

internal static class SweepLineTriangulator2
{
    internal static Graph Triangulate(Profile2D profile, double tolerance = 1e-5)
    {
        profile = profile.WithoutPeripheralVoids();
        var graph = Graph.Create(profile);

        var orderedNodes = graph.Nodes.OrderBy(x => x.Point.X).ToList();
        //Dictionary<GraphNode, (double StartAngle, double EndAngle)> internalAngleRange = CalculateInternalAngleRanges(graph.Nodes);

        var traversedNodes = new List<GraphNode>();
        var traversedEdges = new HashSet<Edge>();

        // Traversal from left to right
        // Each loop run is intended to give one triangle
        foreach(var node in orderedNodes )
        {
            var nodeX = node.Point.X;
            var pastEdges = node.Edges
                .Where(x => x.Start.Point.X.LessThanORAlmostEquals(nodeX)
                    && x.End.Point.X.LessThanORAlmostEquals(nodeX))
                .ToList();
            pastEdges.ForEach(x => traversedEdges.Add(x));
            var traversedNeighbours = node.Neighbours().Where(traversedNodes.Contains).ToList();
            traversedNodes.Add(node);

            // First try to create a triangle by connecting to a neighbour using just one edge
            var connectorEdge = ConnectToNeighbour(traversedNeighbours, node, profile);

            if(connectorEdge != null )
                AddEdgeToGraphAndNodes(graph, connectorEdge);

            // sometimes a node is isolated from traversed edges and needs 2 edges to be placed to make a triangle
            var connectorEdges = ConnectToOtherEdge(traversedEdges, node);
            
            if(connectorEdges != null )
            {
                AddEdgeToGraphAndNodes(graph, connectorEdges.Value.ToStart);
                AddEdgeToGraphAndNodes(graph, connectorEdges.Value.ToEnd);
            }
        }

        return graph;
    }

    private static void AddEdgeToGraphAndNodes(Graph graph, Edge edge)
    {
        edge.Start.Edges.Add(edge);
        edge.End.Edges.Add(edge);
        graph.Edges.Add(edge);
    }

    private static (Edge ToStart, Edge ToEnd)? ConnectToOtherEdge(
        HashSet<Edge> edges,
        GraphNode node)
    {
        foreach (var edge in edges)
        {
            var lineToStart = new Line(node.Point, edge.Start.Point);
            if (edge.Start.Intersects(lineToStart))
                continue;

            var lineToEnd = new Line(node.Point, edge.End.Point);
            if (edge.End.Intersects(lineToEnd))
                continue;

            return (
                ToStart: new Edge(node, edge.Start),
                ToEnd: new Edge(node, edge.End));
        }

        return null;
    }

    private static Edge? ConnectToNeighbour(
        List<GraphNode> traversedNeighbours, 
        GraphNode node,
        Profile2D profile)
    {
        var angleToX = node.Point.FlatAngleToX();

        foreach (var neighbour in traversedNeighbours)
        {
            var closestNeighboursByAngle = neighbour.Neighbours()
                .Except([node])
                .OrderBy(n => Math.Abs(angleToX - n.Point.FlatAngleToX()))
                .Take(2)
                .ToList();

            foreach (var targetNode in closestNeighboursByAngle)
            {
                var line = new Line(node.Point, targetNode.Point);

                if (profile.Contains(line))
                   return new Edge(node, targetNode);
            }
        }

        return null;
    }
}