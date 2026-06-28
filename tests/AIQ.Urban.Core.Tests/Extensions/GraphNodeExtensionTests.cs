//using System.Drawing;
//using System.Linq;
//using AIQ.Urban.Core.Extensions;
//using AIQ.Urban.Core.Models.UrbanGraph;
//using AIQ.Urban.DebugViz;
//using NUnit.Framework;
//using Rhino;
//using Rhino.Geometry;
//using Rhino.Testing.Fixtures;

//namespace AIQ.Urban.Core.Tests.Extensions;

//[RhinoTestFixture]
//public class GraphNodeExtensionTests
//{
//    [Test]
//    [TestCase("CrossingEdge")]
//    [TestCase("TouchingStart")]
//    [TestCase("TouchingEnd")]
//    public void Intersects_WhenLineHitsEdge_ReturnsTrue(string testName)
//    {
//        // Arrange
//        var testData = _intersectingTestData.Single(x => x.CaseName == testName);

//        // Act
//        var result = testData.Node.Intersects(testData.Line);

//        // Visualise
//#if DEBUG
//        VisualiseIntersectionTest(testData.Node, testData.Line, result);
//#endif

//        // Assert
//        Assert.That(result, Is.True);
//    }

//    [Test]
//    [TestCase("ParallelNoIntersect")]
//    [TestCase("AwayNoIntersect")]
//    [TestCase("EmptyNode")]
//    public void Intersects_WhenLineMisses_ReturnsFalse(string testName)
//    {
//        // Arrange
//        var testData = _nonIntersectingTestData.Single(x => x.CaseName == testName);

//        // Act
//        var result = testData.Node.Intersects(testData.Line);

//        // Visualise
//#if DEBUG
//        VisualiseIntersectionTest(testData.Node, testData.Line, result);
//#endif

//        // Assert
//        Assert.That(result, Is.False);
//    }

//#if DEBUG
//    private static void VisualiseIntersectionTest(GraphNode node, Line line, bool intersects)
//    {
//        var doc = RhinoDoc.CreateHeadless(null);
//        var nodeAttribs = RhinoHelper.MakeAttributes(doc, "Node", Color.Red);
//        var edgeAttribs = RhinoHelper.MakeAttributes(doc, "Edge", Color.Black);
//        var lineAttribs = RhinoHelper.MakeAttributes(doc, "TestLine", intersects ? Color.Green : Color.Orange);

//        doc.Objects.AddPoint(node.Point, nodeAttribs);
//        foreach (var edge in node.Edges)
//            doc.Objects.AddLine(edge.ToLine(), edgeAttribs);
//        doc.Objects.AddLine(line, lineAttribs);
//        Visualizer.View(doc);
//    }
//#endif

//    private record IntersectTestData(string CaseName, GraphNode Node, Line Line);

//    private static GraphNode MakeNodeWithEdges(Point3d[] points)
//    {
//        var node = new GraphNode(points[0], []);
//        for (int i = 1; i < points.Length; i++)
//        {
//            var other = new GraphNode(points[i], []);
//            node.Edges.Add(new Edge(node, other));
//        }
//        return node;
//    }

//    private static readonly IntersectTestData[] _intersectingTestData =
//    [
//        new("CrossingEdge",
//            MakeNodeWithEdges([new(5, 5, 0), new(5, -5, 0)]),
//            new Line(new Point3d(3, 0, 0), new Point3d(7, 0, 0))),
//        new("TouchingStart",
//            MakeNodeWithEdges([new(0, 0, 0), new(10, 0, 0)]),
//            new Line(new Point3d(0, 0, 0), new Point3d(0, 5, 0))),
//        new("TouchingEnd",
//            MakeNodeWithEdges([new(0, 0, 0), new(10, 0, 0)]),
//            new Line(new Point3d(10, 0, 0), new Point3d(10, 5, 0))),
//    ];

//    private static readonly IntersectTestData[] _nonIntersectingTestData =
//    [
//        new("ParallelNoIntersect",
//            MakeNodeWithEdges([new(0, 0, 0), new(10, 0, 0)]),
//            new Line(new Point3d(0, 5, 0), new Point3d(10, 5, 0))),
//        new("AwayNoIntersect",
//            MakeNodeWithEdges([new(0, 0, 0), new(10, 0, 0)]),
//            new Line(new Point3d(20, 0, 0), new Point3d(30, 0, 0))),
//        new("EmptyNode",
//            new GraphNode(new Point3d(0, 0, 0), []),
//            new Line(new Point3d(-5, 0, 0), new Point3d(5, 0, 0))),
//    ];
//}
