using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace AIQ.Urban.Core.Models.UrbanGraph;
internal class GraphBuilder(double _approachTolerance = 1e-5, double _tolerance = 1e-5)
{
    private List<Parcel> _parcels = [];
    private List<Building> _buildings = [];
    private List<Transit> _transit = [];
    
    // Initially store straight intersections, then merge based on point?
    private sealed record NodeData(Point3d Point, HashSet<IGraphableAsEdge> Items);

    internal GraphBuilder WithParcels(List<Parcel> parcels)
    {
        _parcels = parcels;
        return this;
    }

    internal GraphBuilder WithBuildings(List<Building> buildings)
    {
        _buildings = buildings;
        return this;
    }

    internal GraphBuilder WithTransit(List<Transit> transit)
    {
        _transit = transit;
        return this;
    }

    internal Graph Run()
    {
        if (_transit.Count == 0)
            return new Graph([], []);

        var graph = RunTransitStep(_transit, _tolerance, _approachTolerance);

        if (_parcels.Count == 0)
            return graph;

        graph = RunProximalParcelsStep(graph, _parcels, _tolerance);

        if(_buildings.Count == 0)
            return graph;

        return RunContainedBuildingsStep(graph, _buildings, _tolerance);
    }

    internal static Graph RunProximalParcelsStep(Graph graph, List<Parcel> parcels, double tolerance = 1e-5)
    {
        var transitItems = new List<(Edge Edge, Transit Source)>();
        
        foreach(var item in graph.Edges)
        {
            if (item.SourceElement is not Transit transit)
                continue;

            transitItems.Add((item, transit));
        }

        foreach(var parcel in parcels)
        {
            var parcelNode = new GraphNode(parcel, []);
            graph.Nodes.Add(parcelNode);

            foreach (var transitItem in transitItems)
            {
                var distanceToCheck = transitItem.Source.Width / 2 + tolerance;
                var curve = transitItem.Edge.Curve.Value;

                bool areProximal = parcel.Outline.Geometry.ClosestPoint(curve.PointAtMid, out double param, distanceToCheck);

                if (!areProximal)
                    continue;

                var startEdge = new Edge(transitItem.Edge.Start, parcelNode);
                var endEdge = new Edge(transitItem.Edge.End, parcelNode);

                parcelNode.Edges.Add(startEdge);
                parcelNode.Edges.Add(endEdge);

                graph.Edges.Add(startEdge);
                graph.Edges.Add(endEdge);
            }
        }

        return graph;
    }

    internal static Graph RunContainedBuildingsStep(Graph graph, List<Building> buildings, double tolerance = 1e-5)
    {
        var parcels = graph.Nodes
            .Where(x => x.SourceObject is Parcel)
            .ToDictionary(x => x, x => x.SourceObject as Parcel);

        foreach (var building in buildings)
        {
            var buildingNode = new GraphNode(building, []);
            graph.Nodes.Add(buildingNode);
            foreach(var parcel in parcels)
            {
                var isWithin = parcel.Value.Outline.Geometry.Contains(building.NodePoint, Plane.WorldXY, tolerance);
                if (isWithin != PointContainment.Inside)
                    continue;

                var edge = new Edge(parcel.Key, buildingNode);
                graph.Edges.Add(edge);
                break;
            }
        }

        return graph;
    }
    internal static Graph RunTransitStep(List<Transit> sourceObjects, double tolerance = 1e-5, double approachTolerance = 1e-5)
    {
        List<NodeData> nodeData = [];

        for (int i = 0; i < sourceObjects.Count; i++)
        {
            var sourceA = sourceObjects[i];


            for (int j = i + 1; j < sourceObjects.Count; j++)
            {
                var sourceB = sourceObjects[j];

                var intersections = Intersection.CurveCurve(
                    sourceA.GraphableGeometry,
                    sourceB.GraphableGeometry,
                    approachTolerance,
                    tolerance);

                foreach (var intersection in intersections)
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

        nodeData = MergeNodeData(nodeData, tolerance);

        var nodesAndData = BuildNodes(nodeData);
        var edges = BuildEdges(sourceObjects.Cast<IGraphableAsEdge>().ToList(), nodesAndData).Distinct().ToList();
        var nodes = nodesAndData.Select(x => x.Node).Distinct().ToList();

        return new Graph(nodes, edges);
    }

    private static List<(GraphNode Node, NodeData Data)> BuildNodes(List<NodeData> nodeData)
    {
        return nodeData
            .Select(x => (new GraphNode(x.Point, []), x))
            .ToList();
    }

    private static List<Edge> BuildEdges(List<IGraphableAsEdge> sources, List<(GraphNode Node, NodeData Data)> nodesAndData)
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
            for (int i = 0; i < group.Nodes.Count - 1; i++)
            {
                var startNode = group.Nodes[i];
                var endNode = group.Nodes[i + 1];

                var edge = new Edge(startNode, endNode, group.Source);
                edges.Add(edge);
            }
        }

        return edges;
    }

    private static (IGraphableAsEdge, List<GraphNode>) OrderAlongSource(List<GraphNode> nodes, IGraphableAsEdge source)
    {
        var curve = source.GraphableGeometry;
        List<GraphNode> orderedNodes = nodes
            .OrderBy(x => {
                curve.ClosestPoint(x.Point, out double param);
                return param;
            })
            .ToList();

        return (source, orderedNodes);
    }

    private static List<GraphNode> FindNodesWithSource(
        List<(GraphNode Node, NodeData Data)> nodesAndData,
        IGraphableAsEdge source)
    {
        return nodesAndData
            .Where(nd => nd.Data.Items.Contains(source))
            .Select(x => x.Node)
            .ToList();
    }

    // Combine Meetings based on points
    private static List<NodeData> MergeNodeData(List<NodeData> nodeData, double tolerance)
    {
        for (int i = 0; i < nodeData.Count - 1; i++)
        {
            var nodeA = nodeData[i];
            for (int j = i + 1; j < nodeData.Count; j++)
            {
                var nodeB = nodeData[j];

                if (!nodeA.Point.EpsilonEquals(nodeB.Point, tolerance))
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
