using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.DebugViz;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Models.UrbanGraph;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using AIQ.Primitives.Extensions;

namespace AIQ.Urban.Core.Tests.Models.UrbanGraph;

[RhinoTestFixture]
public class GraphBuilderTransitTests
{
    [SetUp]
    public void SetUp() => UrbanModel.Instance.Reset();

    private record GraphBuilderTestData(
        string CaseName,
        (double X, double Y)[][] InputCoords,
        NodeDescriptor[] ExpectedNodes,
        EdgeDescriptor[] ExpectedEdges);

    private record NodeDescriptor(double X, double Y);

    private record EdgeDescriptor(
        double StartX, double StartY,
        double EndX, double EndY);

    private static NodeDescriptor N(double x, double y) => new(x, y);
    private static EdgeDescriptor E(double sx, double sy, double ex, double ey) =>
        new(sx, sy, ex, ey);

    private static List<GraphBuilderTestData> BuildTestData()
    {
        return
        [
            new("EmptyTransitList",
                InputCoords: [],
                ExpectedNodes: [],
                ExpectedEdges: []),

            new("SingleTransit",
                InputCoords: [[(0, 0), (10, 0)]],
                ExpectedNodes: [N(0, 0), N(10, 0)],
                ExpectedEdges: [E(0, 0, 10, 0)]),

            new("NoIntersections",
                InputCoords: [
                    [(0, 0), (10, 0)],
                    [(0, 5), (10, 5)],
                ],
                ExpectedNodes: [N(0, 0), N(10, 0), N(0, 5), N(10, 5)],
                ExpectedEdges: [
                    E(0, 0, 10, 0),
                    E(0, 5, 10, 5),
                ]),

            new("TwoCrossingLines",
                InputCoords: [
                    [(0, 0), (10, 0)],
                    [(5, -5), (5, 5)],
                ],
                ExpectedNodes: [N(5, 0), N(0, 0), N(10, 0), N(5, -5), N(5, 5)],
                ExpectedEdges: [
                    E(0, 0, 5, 0),
                    E(5, 0, 10, 0),
                    E(5, -5, 5, 0),
                    E(5, 0, 5, 5),
                ]),

            new("ThreeLinesCommonJunction",
                InputCoords: [
                    [(-5, 0), (5, 0)],
                    [(0, -5), (0, 5)],
                    [(-5, -5), (5, 5)],
                ],
                ExpectedNodes: [N(0, 0), N(-5, 0), N(5, 0), N(0, -5), N(0, 5), N(-5, -5), N(5, 5)],
                ExpectedEdges: [
                    E(-5, 0, 0, 0),
                    E(0, 0, 5, 0),
                    E(0, -5, 0, 0),
                    E(0, 0, 0, 5),
                    E(-5, -5, 0, 0),
                    E(0, 0, 5, 5),
                ]),

            new("SquareWithCross",
                InputCoords: [
                    [(-5, -5), (5, -5)],
                    [(5,-5), (5, 5)],
                    [(5,5), (-5, 5)],
                    [(-5,5),(-5, -5)],
                    [(-5, 0), (5, 0)],
                    [(0, -5), (0, 5)],
                ],
                ExpectedNodes: [
                    N(-5, 0), 
                    N(5, 0),
                    N(0, -5),
                    N(0, 5),
                    N(0, 0),
                    N(-5, -5), 
                    N(5, -5), 
                    N(5, 5), 
                    N(-5, 5),
                ],
                ExpectedEdges: [
                    E(-5, -5, 0, -5),
                    E(0, -5, 5, -5),
                    E(5, -5, 5, 0),
                    E(5, 0, 5, 5),
                    E(5, 5, 0, 5),
                    E(0, 5, -5, 5),
                    E(-5, 5, -5, 0),
                    E(-5, 0, -5, -5),

                    E(-5, 0, 0, 0),
                    E(0, 0, 5, 0),
                    E(0, -5, 0, 0),
                    E(0, 0, 0, 5),
                ]),
        ];
    }

    private static readonly Lazy<List<GraphBuilderTestData>> _testData = new(BuildTestData);

    [TestCase("EmptyTransitList")]
    [TestCase("SingleTransit")]
    [TestCase("NoIntersections")]
    [TestCase("TwoCrossingLines")]
    [TestCase("ThreeLinesCommonJunction")]
    [TestCase("SquareWithCross")]
    public void RunTransitStep_produces_correct_graph(string testName)
    {
        // Arrange
        var data = _testData.Value.Single(d => d.CaseName == testName);
        var transits = data.InputCoords
            .Select(coords => MakeTransit(coords))
            .ToList();

        // Act
        var graph = GraphBuilder.RunTransitStep(transits);

        // Visualise
#if DEBUG
        graph.VisualizeGraph();
#endif

        // Assert
        Assert.That(graph.Nodes.Count, Is.EqualTo(data.ExpectedNodes.Length),
            $"{data.CaseName}: node count mismatch. Expected {data.ExpectedNodes.Length}, got {graph.Nodes.Count}");
        Assert.That(graph.Edges.Count, Is.EqualTo(data.ExpectedEdges.Length),
            $"{data.CaseName}: edge count mismatch. Expected {data.ExpectedEdges.Length}, got {graph.Edges.Count}");

        foreach(var node in data.ExpectedNodes)
        {
            bool pass = graph.Nodes.Exists(n => n.Point.X.AlmostEquals(node.X) && n.Point.Y.AlmostEquals(node.Y));
            Assert.True(pass, $"Node with X:{node.X} and Y:{node.Y} not found");
        }

        foreach (var edge in data.ExpectedEdges)
        {
            bool startPass = graph.Edges.Exists(e => e.Curve.Value.PointAtStart.X.AlmostEquals(edge.StartX) && e.Curve.Value.PointAtStart.Y.AlmostEquals(edge.StartY));
            Assert.True(startPass, $"Edge with starts X:{edge.StartX} and Y:{edge.StartY} not found");

            bool endPass = graph.Edges.Exists(e => e.Curve.Value.PointAtEnd.X.AlmostEquals(edge.EndX) && e.Curve.Value.PointAtEnd.Y.AlmostEquals(edge.EndY));
            Assert.True(endPass, $"Edge with X:{edge.EndX} and Y:{edge.EndY} not found");
        }
    }

    private static Transit MakeTransit((double X, double Y)[] coords)
    {
        var curve = Curve2D.Create(coords.ToList());
        return new Transit(curve, 0.5, TransitType.Road);
    }
}
