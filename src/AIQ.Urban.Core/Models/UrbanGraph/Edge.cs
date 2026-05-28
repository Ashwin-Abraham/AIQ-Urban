using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanGraph;
public class Edge(IGraphable source, GraphNode start, GraphNode end)
{
    public IGraphable SourceElement { get; } = source;
    public GraphNode Start { get; } = start;
    public GraphNode End { get; } = end;
}
