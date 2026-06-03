using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Models.UrbanGraph;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Models.UrbanGraph;

//[RhinoTestFixture]
public class GraphBuilderTransitTests
{
    [Test]
    public void DummyTest()
    {
        int a = 7;
        int b = a + 8;
    }

    [Test]
    [TestCase("TwoCrossingLines")]
    [TestCase("NoIntersections")]
    [TestCase("SingleTransit")]
    [TestCase("EmptyTransitList")]
    [TestCase("ThreeLinesCommonJunction")]
    [TestCase("SquareWithCross")]
    public void RunTransitStep_CreatesCorrectGraph(string testName)
    {
        var testData = _testData.Value.Single(x => x.CaseName == testName);

        // Arrange
        var graph = GraphBuilder.RunTransitStep(testData.Input, _tolerance, _approachTolerance);

        // Assert
        AssertGraphStructure(graph, testData.ExpectedNodes, testData.ExpectedEdges, _tolerance);
    }

    private static void AssertGraphStructure(
        Graph graph,
        List<NodeDescriptor> expectedNodes,
        List<EdgeDescriptor> expectedEdges,
        double tolerance)
    {
        Assert.That(graph.Nodes, Has.Count.EqualTo(expectedNodes.Count));
        Assert.That(graph.Edges, Has.Count.EqualTo(expectedEdges.Count));

        var matchedNodes = new List<(Point3d Expected, GraphNode Actual)>(expectedNodes.Count);
        foreach (var expected in expectedNodes)
        {
            var match = graph.Nodes.FirstOrDefault(n => n.Point.EpsilonEquals(expected.Point, tolerance));
            Assert.That(match, Is.Not.Null, $"Expected node at ({expected.Point.X}, {expected.Point.Y}) not found");
            matchedNodes.Add((expected.Point, match));
        }

        foreach (var expected in expectedEdges)
        {
            var startActual = matchedNodes.First(m => m.Expected.EpsilonEquals(expected.A, tolerance)).Actual;
            var endActual = matchedNodes.First(m => m.Expected.EpsilonEquals(expected.B, tolerance)).Actual;

            var edgeExists = graph.Edges.Any(e =>
                (ReferenceEquals(e.Start, startActual) && ReferenceEquals(e.End, endActual)) ||
                (ReferenceEquals(e.Start, endActual) && ReferenceEquals(e.End, startActual)));

            Assert.That(edgeExists, Is.True,
                $"Expected edge between ({expected.A.X}, {expected.A.Y}) and ({expected.B.X}, {expected.B.Y}) not found");
        }
    }

    private record NodeDescriptor(Point3d Point);

    private record EdgeDescriptor(Point3d A, Point3d B);

    private record GraphBuilderTestData(
        string CaseName,
        List<Transit> Input,
        List<NodeDescriptor> ExpectedNodes,
        List<EdgeDescriptor> ExpectedEdges);

    private const double _tolerance = 1e-5;
    private const double _approachTolerance = 1e-5;

    private static readonly Lazy<List<GraphBuilderTestData>> _testData = new(BuildTestData);

    private static Point3d P(double x, double y) => new(x, y, 0);

    private static Transit MakeTransit(params (double X, double Y)[] coords)
    {
        var curve = Curve2D.Create(coords.ToList());
        return new Transit(curve, 0.01, TransitType.Road);
    }

    private static List<EdgeDescriptor> EdgesAlong(params Point3d[] points)
    {
        var edges = new List<EdgeDescriptor>(points.Length - 1);
        for (int i = 0; i < points.Length - 1; i++)
            edges.Add(new EdgeDescriptor(points[i], points[i + 1]));
        return edges;
    }

    private static List<GraphBuilderTestData> BuildTestData()
    {
        return
        [
            new GraphBuilderTestData(
                CaseName: "TwoCrossingLines",
                Input:
                [
                    MakeTransit((0, 0), (10, 0)),
                    MakeTransit((5, -5), (5, 5)),
                ],
                ExpectedNodes:
                [
                    new NodeDescriptor(P(0, 0)),
                    new NodeDescriptor(P(10, 0)),
                    new NodeDescriptor(P(5, 5)),
                    new NodeDescriptor(P(5, -5)),
                    new NodeDescriptor(P(5, 0)),
                ],
                ExpectedEdges:
                [
                    .. EdgesAlong(P(0, 0), P(5, 0), P(10, 0)),
                    .. EdgesAlong(P(5, -5), P(5, 0), P(5, 5)),
                ]),

            new GraphBuilderTestData(
                CaseName: "NoIntersections",
                Input:
                [
                    MakeTransit((0, 0), (10, 0)),
                    MakeTransit((0, 5), (10, 5)),
                ],
                ExpectedNodes:
                [
                    new NodeDescriptor(P(0, 0)),
                    new NodeDescriptor(P(10, 0)),
                    new NodeDescriptor(P(0, 5)),
                    new NodeDescriptor(P(10, 5)),
                ],
                ExpectedEdges:
                [
                    .. EdgesAlong(P(0, 0), P(10, 0)),
                    .. EdgesAlong(P(0, 5), P(10, 5)),
                ]),

            new GraphBuilderTestData(
                CaseName: "SingleTransit",
                Input:
                [
                    MakeTransit((0, 0), (10, 0)),
                ],
                ExpectedNodes:
                [
                    new NodeDescriptor(P(0, 0)),
                    new NodeDescriptor(P(10, 0)),
                ],
                ExpectedEdges:
                [
                    .. EdgesAlong(P(0, 0), P(10, 0)),
                ]),

            new GraphBuilderTestData(
                CaseName: "EmptyTransitList",
                Input: [],
                ExpectedNodes: [],
                ExpectedEdges: []),

            new GraphBuilderTestData(
                CaseName: "ThreeLinesCommonJunction",
                Input:
                [
                    MakeTransit((0, 0), (10, 0)),
                    MakeTransit((5, -5), (5, 5)),
                    MakeTransit((2, 5), (8, -5)),
                ],
                ExpectedNodes:
                [
                    new NodeDescriptor(P(0, 0)),
                    new NodeDescriptor(P(10, 0)),
                    new NodeDescriptor(P(5, -5)),
                    new NodeDescriptor(P(5, 5)),
                    new NodeDescriptor(P(2, 5)),
                    new NodeDescriptor(P(8, -5)),
                    new NodeDescriptor(P(5, 0)),
                ],
                ExpectedEdges:
                [
                    .. EdgesAlong(P(0, 0), P(5, 0), P(10, 0)),
                    .. EdgesAlong(P(5, -5), P(5, 0), P(5, 5)),
                    .. EdgesAlong(P(2, 5), P(5, 0), P(8, -5)),
                ]),

            new GraphBuilderTestData(
                CaseName: "SquareWithCross",
                Input:
                [
                    MakeTransit((0, 0), (10, 0)),
                    MakeTransit((10, 0), (10, 10)),
                    MakeTransit((10, 10), (0, 10)),
                    MakeTransit((0, 10), (0, 0)),
                    MakeTransit((5, 0), (5, 10)),
                    MakeTransit((0, 5), (10, 5)),
                ],
                ExpectedNodes:
                [
                    new NodeDescriptor(P(0, 0)),
                    new NodeDescriptor(P(10, 0)),
                    new NodeDescriptor(P(10, 10)),
                    new NodeDescriptor(P(0, 10)),
                    new NodeDescriptor(P(5, 0)),
                    new NodeDescriptor(P(5, 10)),
                    new NodeDescriptor(P(0, 5)),
                    new NodeDescriptor(P(10, 5)),
                    new NodeDescriptor(P(5, 5)),
                ],
                ExpectedEdges:
                [
                    .. EdgesAlong(P(0, 0), P(5, 0), P(10, 0)),
                    .. EdgesAlong(P(10, 0), P(10, 5), P(10, 10)),
                    .. EdgesAlong(P(10, 10), P(5, 10), P(0, 10)),
                    .. EdgesAlong(P(0, 10), P(0, 5), P(0, 0)),
                    .. EdgesAlong(P(5, 0), P(5, 5), P(5, 10)),
                    .. EdgesAlong(P(0, 5), P(5, 5), P(10, 5)),
                ]),
        ];
    }
}
