using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanGraph;
public class Graph(List<GraphNode> nodes, List<Edge> edges)
{
    public List<GraphNode> Nodes { get; } = nodes;
    public List<Edge> Edges { get; } = edges;

    private record Vertex(Point3d Point);

    public static Graph Create(Profile2D profile)
    {
        List<List<Vertex>> profileVertices = ConvertToVertices(profile);
        Dictionary<Vertex, Vertex?> sharedVerticesMap = ComputeSharedVertices(profileVertices);
        return BuildGraph(profileVertices, sharedVerticesMap);
    }

    private static Graph BuildGraph(
        List<List<Vertex>> profileVertices,
        Dictionary<Vertex, Vertex?> preferredVertexMap)
    {
        List<Edge> edges = [];
        Dictionary<Vertex, GraphNode> nodeVertexMap = [];

        foreach (var vertexLoop in profileVertices)
        {
            var vertex = vertexLoop[0];
            var preferredVertex = preferredVertexMap[vertex] ?? vertex;

            if (!nodeVertexMap.TryGetValue(preferredVertex, out var previousNode))
                previousNode = new GraphNode(vertex.Point, []);

            var firstNode = previousNode;
            GraphNode? thisNode = null;

            for (int i = 1; i < vertexLoop.Count; i++)
            {
                vertex = vertexLoop[i];
                preferredVertex = preferredVertexMap[vertex] ?? vertex;

                if (!nodeVertexMap.TryGetValue(preferredVertex, out thisNode))
                    thisNode = new GraphNode(vertex.Point, []);

                edges.Add(new Edge(previousNode, thisNode));
                previousNode = thisNode;
            }

            if (thisNode != null)
                edges.Add(new Edge(thisNode, firstNode));
        }

        var nodes = nodeVertexMap.Values.ToList();

        return new Graph(nodes, edges);
    }

    /// <summary>
    /// Key = the vertex
    /// Value = Nullable, It is the vertex which dominates this Key. Ie use the Value if present
    /// </summary>
    private static Dictionary<Vertex, Vertex?> ComputeSharedVertices(List<List<Vertex>> profileVertices, double tolerance = 1e-5)
    {
        var vertices = profileVertices.SelectMany(x => x).ToList();
        Dictionary<Vertex, Vertex?> vertexSharingMap = [];

        foreach (var vertex in vertices)
        {
            var hit = vertexSharingMap.Keys.FirstOrDefault(v => v.Point.EpsilonEquals(vertex.Point, tolerance));

            // If no previous vertex at this position, there is no Value which dominantes it.


            vertexSharingMap.Add(vertex, hit);

            // If there was a previous vertex here, then add the Value as the dominant
        }

        return vertexSharingMap;
    }

    private static List<List<Vertex>> ConvertToVertices(Profile2D profile)
    {
        var verticesLists = profile.Holes
            .Select(p => ConvertToVertices(p))
            .ToList();

        verticesLists.Add(ConvertToVertices(profile.Outline));
        return verticesLists;
    }

    private static List<Vertex> ConvertToVertices(Polygon2D polygon)
    {
        return polygon.Points()
            .Select(p => new Vertex(p))
            .ToList();
    }
}