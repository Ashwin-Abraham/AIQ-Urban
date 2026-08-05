using System;
using System.Drawing;
using System.Linq;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Extensions;

[RhinoTestFixture]
public class Polygon2DExtensionTests
{

    [Test]
    [TestCase("LineInsidePolygon")]
    [TestCase("LineOutsidePolygon")]
    [TestCase("LineCrossingBoundary")]
    [TestCase("LineOnEdge")]
    [TestCase("LineOnMultiEdgePolygon")]
    public void PolygonContains_Line(string testName)
    {
        // Arrange
        var td = GetPolygonContainsLineTestData(testName);
        var polygon = Polygon2D.Create(td.Outline.Select(v => (v[0], v[1])).ToList());
        var line = new Line(new Point3d(td.LineFrom[0], td.LineFrom[1], 0),
                            new Point3d(td.LineTo[0], td.LineTo[1], 0));

        // Act
        var result = polygon.ContainsOrCoincides(line);

#if DEBUG
        // Visualise
        PreviewHub.ResetDoc();
        line.Transform(Transform.Translation(0, 0, .3));
        line.ToNurbsCurve().AddToPreviewDoc(Color.Red);
        polygon.PreviewViaHub(color: Color.Black);
#endif

        // Assert
        Assert.That(result, Is.EqualTo(td.Expected));
    }

    private record PrimitivePolygonContainsLineTestData(
       double[][] Outline,
       double[] LineFrom,
       double[] LineTo,
       bool Expected);

    private static PrimitivePolygonContainsLineTestData GetPolygonContainsLineTestData(string caseName)
    {
        return caseName switch
        {
            "LineInsidePolygon" => new PrimitivePolygonContainsLineTestData(
                Outline: [[0, 0], [10, 0], [10, 10], [0, 10]],
                LineFrom: [1, 5],
                LineTo: [9, 5],
                Expected: true),

            "LineOutsidePolygon" => new PrimitivePolygonContainsLineTestData(
                Outline: [[0, 0], [10, 0], [10, 10], [0, 10]],
                LineFrom: [20, 0],
                LineTo: [30, 0],
                Expected: false),

            "LineCrossingBoundary" => new PrimitivePolygonContainsLineTestData(
                Outline: [[0, 0], [10, 0], [10, 10], [0, 10]],
                LineFrom: [-5, 5],
                LineTo: [15, 5],
                Expected: false),

            "LineOnEdge" => new PrimitivePolygonContainsLineTestData(
                Outline: [[0, 0], [10, 0], [10, 10], [0, 10]],
                LineFrom: [10, 2],
                LineTo: [10, 8],
                Expected: false),

            "LineOnMultiEdgePolygon" => new PrimitivePolygonContainsLineTestData(
                Outline: [[0, 0], [3, 0], [6,0], [10,0], [10, 10], [0, 10]],
                LineFrom: [2, 0],
                LineTo: [9.2, 0],
                Expected: true),

            _ => throw new ArgumentException($"Unknown test case: {caseName}")
        };
    }
}
