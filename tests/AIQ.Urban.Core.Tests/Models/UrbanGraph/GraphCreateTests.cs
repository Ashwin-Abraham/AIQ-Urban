using System.Collections.Generic;
using System.Linq;
using AIQ.PreviewHelper.Core;
using AIQ.Primitives.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanGraph;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Models.UrbanGraph;

[RhinoTestFixture]
public class GraphCreateTests
{
    [Test]
    [TestCase("SimpleSquare")]
    [TestCase("Triangle")]
    [TestCase("LShape")]
    [TestCase("RectangleWithHole")]
    [TestCase("HoleOnCorner")]
    [TestCase("HoleOnEdge")]
    public void Create_FromProfile2D_ProducesCorrectGraph(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        var profile = testData.Input;

        // Act
        var graph = Graph.Create(profile);

#if DEBUG
        // Preview
        PreviewHub.ResetDoc();
        graph.PreviewViaHub();
#endif

        // Assert
        Assert.That(graph.Nodes.Count, Is.EqualTo(testData.ExpectedNodeCount),
            $"{testData.CaseName}: node count mismatch");
        Assert.That(graph.Edges.Count, Is.EqualTo(testData.ExpectedEdgeCount),
            $"{testData.CaseName}: edge count mismatch");

        foreach (var coord in testData.ExpectedNodeCoords)
        {
            Assert.That(graph.Nodes.Any(n =>
                n.Point.X.AlmostEquals(coord.X) && n.Point.Y.AlmostEquals(coord.Y)),
                Is.True, $"{testData.CaseName}: node ({coord.X}, {coord.Y}) not found");
        }
    }

    private record GraphCreateTestData(
        string CaseName,
        Profile2D Input,
        int ExpectedNodeCount,
        int ExpectedEdgeCount,
        (double X, double Y)[] ExpectedNodeCoords);

    private static readonly List<GraphCreateTestData> _testData = BuildTestData();

    private static List<GraphCreateTestData> BuildTestData()
    {
        return
        [
            new(
                CaseName: "SimpleSquare",
                Input: MakeSquare(),
                ExpectedNodeCount: 4,
                ExpectedEdgeCount: 4,
                ExpectedNodeCoords: [(0, 0), (10, 0), (10, 10), (0, 10)]),

            new(
                CaseName: "Triangle",
                Input: MakeTriangle(),
                ExpectedNodeCount: 3,
                ExpectedEdgeCount: 3,
                ExpectedNodeCoords: [(0, 0), (5, 10), (10, 0)]),

            new(
                CaseName: "LShape",
                Input: MakeLShape(),
                ExpectedNodeCount: 6,
                ExpectedEdgeCount: 6,
                ExpectedNodeCoords: [(0, 0), (10, 0), (10, 5), (5, 5), (5, 10), (0, 10)]),

            new(
                CaseName: "RectangleWithHole",
                Input: MakeRectangleWithHole(),
                ExpectedNodeCount: 8,
                ExpectedEdgeCount: 8,
                ExpectedNodeCoords: [(0, 0), (20, 0), (20, 10), (0, 10), (5, 2), (15, 2), (15, 8), (5, 8)]),

            new(
                CaseName: "HoleOnCorner",
                Input: MakeHoleOnCorner(),
                ExpectedNodeCount: 7,
                ExpectedEdgeCount: 8,
                ExpectedNodeCoords: [(0, 0), (20, 0), (20, 20), (0, 20), (10, 0), (10, 10), (0, 10)]),

            new(
                CaseName: "HoleOnEdge",
                Input: MakeHoleOnEdge(),
                ExpectedNodeCount: 8,
                ExpectedEdgeCount: 8,
                ExpectedNodeCoords: [(0, 0), (20, 0), (20, 20), (0, 20), (5, 20), (15, 20), (15, 10), (5, 10)]),
        ];
    }

    private static Profile2D MakeSquare()
    {
        var outline = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        return new Profile2D(outline, []);
    }

    private static Profile2D MakeTriangle()
    {
        var outline = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(5, 10, 0),
            new Point3d(10, 0, 0),
            new Point3d(0, 0, 0),
        ]));
        return new Profile2D(outline, []);
    }

    private static Profile2D MakeLShape()
    {
        var outline = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 5, 0),
            new Point3d(5, 5, 0),
            new Point3d(5, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        return new Profile2D(outline, []);
    }

    private static Profile2D MakeRectangleWithHole()
    {
        var outline = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(20, 0, 0),
            new Point3d(20, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var hole = new Polygon2D(new PolylineCurve([
            new Point3d(5, 2, 0),
            new Point3d(15, 2, 0),
            new Point3d(15, 8, 0),
            new Point3d(5, 8, 0),
            new Point3d(5, 2, 0),
        ]));
        return new Profile2D(outline, [hole]);
    }

    private static Profile2D MakeHoleOnCorner()
    {
        var outline = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(20, 0, 0),
            new Point3d(20, 20, 0),
            new Point3d(0, 20, 0),
            new Point3d(0, 0, 0),
        ]));
        var hole = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        return new Profile2D(outline, [hole]);
    }

    private static Profile2D MakeHoleOnEdge()
    {
        var outline = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(20, 0, 0),
            new Point3d(20, 20, 0),
            new Point3d(0, 20, 0),
            new Point3d(0, 0, 0),
        ]));
        var hole = new Polygon2D(new PolylineCurve([
            new Point3d(5, 20, 0),
            new Point3d(15, 20, 0),
            new Point3d(15, 10, 0),
            new Point3d(5, 10, 0),
            new Point3d(5, 20, 0),
        ]));
        return new Profile2D(outline, [hole]);
    }
}
