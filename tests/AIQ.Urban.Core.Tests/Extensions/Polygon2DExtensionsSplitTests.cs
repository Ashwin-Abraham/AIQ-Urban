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
public class Polygon2DExtensionsSplitTests
{
    [Test]
    [TestCase("CrossingLine")]
    [TestCase("ShallowIntrudingLine")]
    [TestCase("DetachedLine")]
    [TestCase("CrossingArc")]
    [TestCase("CrossingCircle")]
    [TestCase("CrossingClosedPolygon")]
    public void Split_SplitterScenario_ReturnsExpectedRegions(string caseName)
    {
        // Arrange
        var testData = GetTestData(caseName);
        var polygon = Polycurve2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)])!;
        var splitter = new Curve2D(CreateSplitterGeometry(testData), Guid.NewGuid());

        // Act
        var result = polygon.Split([splitter]);

        // Preview
#if DEBUG
        PreviewHub.ResetDoc();
        polygon.PreviewViaHub(color: Color.Black);
        splitter.Geometry.AddToPreviewDoc(Color.Red);
        foreach (var fragment in result)
            fragment.PreviewViaHub();
#endif

        // Assert
        Assert.That(result, Has.Count.EqualTo(testData.ExpectedRegionCount));
        Assert.That(result, Has.All.Matches<Polycurve2D>(fragment =>
            fragment.Geometry.IsValid && fragment.Geometry.IsClosed));

        if (testData.ExpectedRegionCount == 1)
        {
            Assert.That(result[0], Is.SameAs(polygon));
            return;
        }

        Assert.That(result, Has.All.Matches<Polycurve2D>(fragment =>
            fragment.Geometry.GetLength() > 0));
        Assert.That(result, Has.None.SameAs(polygon));
    }

    private static Curve CreateSplitterGeometry(SplitTestData testData)
    {
        var points = testData.Points
            .Select(point => new Point3d(point[0], point[1], 0))
            .ToArray();

        return testData.SplitterType switch
        {
            "Line" => new Line(points[0], points[1]).ToNurbsCurve(),
            "Arc" => new ArcCurve(new Arc(points[0], points[1], points[2])),
            "Circle" => new ArcCurve(new Circle(points[0], testData.Radius)),
            "ClosedPolygon" => new PolylineCurve(points),
            _ => throw new ArgumentException($"Unknown splitter type: {testData.SplitterType}")
        };
    }

    private static SplitTestData GetTestData(string caseName)
    {
        return caseName switch
        {
            "CrossingLine" => new SplitTestData(
                SplitterType: "Line",
                Points: [[-1, 5], [11, 5]],
                Radius: 0,
                ExpectedRegionCount: 2),

            "ShallowIntrudingLine" => new SplitTestData(
                SplitterType: "Line",
                Points: [[-1, 5], [1, 5]],
                Radius: 0,
                ExpectedRegionCount: 1),

            "DetachedLine" => new SplitTestData(
                SplitterType: "Line",
                Points: [[-1, 12], [11, 12]],
                Radius: 0,
                ExpectedRegionCount: 1),

            "CrossingArc" => new SplitTestData(
                SplitterType: "Arc",
                Points: [[-1, 5], [5, 8], [11, 5]],
                Radius: 0,
                ExpectedRegionCount: 2),

            "CrossingCircle" => new SplitTestData(
                SplitterType: "Circle",
                Points: [[5, -1]],
                Radius: 4,
                ExpectedRegionCount: 2),

            "CrossingClosedPolygon" => new SplitTestData(
                SplitterType: "ClosedPolygon",
                Points: [[3, -2], [7, -2], [5, 6], [3, -2]],
                Radius: 0,
                ExpectedRegionCount: 2),

            _ => throw new ArgumentException($"Unknown test case: {caseName}")
        };
    }

    private record SplitTestData(
        string SplitterType,
        double[][] Points,
        double Radius,
        int ExpectedRegionCount);
}
