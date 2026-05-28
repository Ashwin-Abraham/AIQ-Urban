namespace AIQ.Urban.Core.Models.UrbanGraph;
public class Graph(List<GraphNode> nodes, List<Edge> edges)
{
    public List<GraphNode> Nodes { get; } = nodes;
    public List<Edge> Edges { get; } = edges;
}
