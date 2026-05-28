using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Models.UrbanGraph;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace AIQ.Urban.Core.ModelActions;

internal class GraphCreationStep(double _approachTolerance = 1e-5, double _tolerance = 1e-5)
{
    // Initially store straight intersections, then merge based on point?
    private record NodeData(Point3d Point, HashSet<IGraphable> Items);

    internal Graph Run(List<IGraphable> sourceObjects)
    {
        List<NodeData> nodeData = [];

        for (int i = 0; i < sourceObjects.Count; i++)
        {
            var sourceA = sourceObjects[i];


            for(int j = i + 1; j < sourceObjects.Count; j++)
            {
                var sourceB = sourceObjects[j];

                var intersections = Intersection.CurveCurve(
                    sourceA.Geometry,
                    sourceB.Geometry, 
                    _approachTolerance,
                    _tolerance);

                foreach(var intersection in intersections)
                {
                    var intersectionStart = new NodeData(
                        Point: intersection.PointA,
                        Items: [sourceA, sourceB]);

                    nodeData.Add(intersectionStart);

                    if (!intersection.IsOverlap)
                        continue;

                    var intersectionEnd = new NodeData(
                        Point: intersection.PointB,
                        Items: [sourceA, sourceB]);

                    nodeData.Add(intersectionEnd);
                }
            }
        }

        nodeData = MergeNodeData(nodeData);

        var nodesAndData = BuildNodes(nodeData);
        var edges = BuildEdges(sourceObjects, nodesAndData).Distinct().ToList();
        var nodes = nodesAndData.Select(x => x.Node).Distinct().ToList();

        return new Graph(nodes, edges);
    }

    private List<(GraphNode Node, NodeData Data)> BuildNodes(List<NodeData> nodeData)
    {
        return nodeData
            .Select(x => (new GraphNode(x.Point, []), x))
            .ToList();
    }

    private List<Edge> BuildEdges(List<IGraphable> sources, List<(GraphNode Node, NodeData Data)> nodesAndData)
    {
        var nodesPerSource = sources
            .Select(src => (Source: src, Nodes: FindNodesWithSource(nodesAndData, src)))
            .ToList();

        // Order the nodes along graphable's curve
        nodesPerSource = nodesPerSource
            .Select(x => OrderAlongSource(x.Nodes, x.Source))
            .ToList();

        List<Edge> edges = [];

        foreach (var group in nodesPerSource)
        {
            for(int i = 0; i < group.Nodes.Count; i++)
            {
                var startNode = group.Nodes[i];
                var endNode = group.Nodes[i + 1];

                var edge = new Edge(group.Source, startNode, endNode);
                edges.Add(edge);
            }
        }

        return edges;
    }

    private (IGraphable, List<GraphNode>) OrderAlongSource(List<GraphNode> nodes, IGraphable source)
    {
        var curve = source.Geometry;
        List<GraphNode> orderedNodes = nodes
            .OrderBy(x => {
                curve.ClosestPoint(x.Point, out double param);
                return param;
                })
            .ToList();

        return (source, orderedNodes);
    }

    private List<GraphNode> FindNodesWithSource(
        List<(GraphNode Node, NodeData Data)> nodesAndData,
        IGraphable source)
    {
        return nodesAndData
            .Where(nd => nd.Data.Items.Contains(source))
            .Select(x => x.Node)
            .ToList();
    }

    // Combine Meetings based on points
    private List<NodeData> MergeNodeData(List<NodeData> nodeData)
    {
        for(int i = 0; i < nodeData.Count - 1; i++)
        {
            var nodeA = nodeData[i];
            for(int j = i + 1; j < nodeData.Count; j++)
            {
                var nodeB = nodeData[j];

                if (!nodeA.Point.EpsilonEquals(nodeB.Point, _tolerance))
                    continue;

                nodeData.Remove(nodeB);
                j--;
                var items = nodeA.Items.Concat(nodeB.Items).Distinct().ToHashSet();
                nodeData[i] = new NodeData(nodeA.Point, items);
            }
        }

        return nodeData;
    }
}
