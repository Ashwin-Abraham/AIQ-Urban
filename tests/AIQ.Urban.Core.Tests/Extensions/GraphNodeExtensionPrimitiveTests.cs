using System;
using System.Drawing;
using System.Linq;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.UrbanGraph;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Extensions;

[RhinoTestFixture]
public class GraphNodeExtensionPrimitiveTests
{
    [Test]
    [TestCase("CrossingEdge")]
    [TestCase("TouchingStart")]
    [TestCase("TouchingEnd")]
    [TestCase("ParallelNoIntersect")]
    [TestCase("AwayNoIntersect")]
    [TestCase("EmptyNode")]
    public void Intersects_ReturnsExpectedResult(string testName)
    {
        // Arrange
        var testData = GetPrimitiveTestData(testName);
        var node = MakeNodeWithEdges(testData.NodeCoords.Select(c => new Point3d(c[0], c[1], c[2])).ToArray());
        var line = new Line(new Point3d(testData.LineFrom[0], testData.LineFrom[1], testData.LineFrom[2]),
                            new Point3d(testData.LineTo[0], testData.LineTo[1], testData.LineTo[2]));

        // Act
        var result = node.Intersects(line);

        // Visualise
#if DEBUG
        VisualiseIntersectionTest(node, line, result);
#endif

        // Assert
        Assert.That(result, Is.EqualTo(testData.Expected));
    }

#if DEBUG
    private static void VisualiseIntersectionTest(GraphNode node, Line line, bool intersects)
    {
        var doc = RhinoDoc.CreateHeadless(null);
        var nodeAttribs = RhinoHelper.MakeAttributes(doc, "Node", Color.Red);
        var edgeAttribs = RhinoHelper.MakeAttributes(doc, "Edge", Color.Black);
        var lineAttribs = RhinoHelper.MakeAttributes(doc, "TestLine", intersects ? Color.Green : Color.Orange);

        doc.Objects.AddPoint(node.Point, nodeAttribs);
        foreach (var edge in node.Edges)
            doc.Objects.AddLine(edge.ToLine(), edgeAttribs);
        doc.Objects.AddLine(line, lineAttribs);
        Visualizer.View(doc);
    }
#endif

    private static GraphNode MakeNodeWithEdges(Point3d[] points)
    {
        var node = new GraphNode(points[0], []);
        for (int i = 1; i < points.Length; i++)
        {
            var other = new GraphNode(points[i], []);
            node.Edges.Add(new Edge(node, other));
        }
        return node;
    }

    private record PrimitiveIntersectTestCase(string CaseName, double[][] NodeCoords, double[] LineFrom, double[] LineTo, bool Expected);

    private static PrimitiveIntersectTestCase GetPrimitiveTestData(string caseName)
    {
        return caseName switch
        {
            "CrossingEdge" => new PrimitiveIntersectTestCase(
                CaseName: "CrossingEdge",
                NodeCoords: [[5, 5, 0], [5, -5, 0]],
                LineFrom: [3, 0, 0],
                LineTo: [7, 0, 0],
                Expected: true),

            "TouchingStart" => new PrimitiveIntersectTestCase(
                CaseName: "TouchingStart",
                NodeCoords: [[0, 0, 0], [10, 0, 0]],
                LineFrom: [0, 0, 0],
                LineTo: [0, 5, 0],
                Expected: true),
            
            "TouchingEnd" => new PrimitiveIntersectTestCase(
                CaseName: "TouchingEnd",
                NodeCoords: [[0, 0, 0], [10, 0, 0]],
                LineFrom: [10, 0, 0],
                LineTo: [10, 5, 0],
                Expected: true),
            
            "ParallelNoIntersect" => new PrimitiveIntersectTestCase(
                CaseName: "ParallelNoIntersect",
                NodeCoords: [[0, 0, 0], [10, 0, 0]],
                LineFrom: [0, 5, 0],
                LineTo: [10, 5, 0],
                Expected: false),
            
            "AwayNoIntersect" => new PrimitiveIntersectTestCase(
                CaseName: "AwayNoIntersect",
                NodeCoords: [[0, 0, 0], [10, 0, 0]],
                LineFrom: [20, 0, 0],
                LineTo: [30, 0, 0],
                Expected: false),
            
            "EmptyNode" => new PrimitiveIntersectTestCase(
                CaseName: "EmptyNode",
                NodeCoords: [[0, 0, 0]],
                LineFrom: [-5, 0, 0],
                LineTo: [5, 0, 0],
                Expected: false),

            _ => throw new ArgumentException($"Unknown test case: {caseName}")
        };
    }
}
