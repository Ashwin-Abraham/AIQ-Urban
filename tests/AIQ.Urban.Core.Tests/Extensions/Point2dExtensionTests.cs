using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Extensions;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Extensions;

[RhinoTestFixture]
public class Point2dExtensionTests
{
    [Test]
    [TestCase("SinglePoint")]
    [TestCase("TwoPoints")]
    [TestCase("CollinearPoints")]
    public void GrahamConvexHull_ReturnsFewerThanThreePoints(string testName)
    {
        // Arrange
        var testData = _degenerateTestData.Single(x => x.CaseName == testName);
        
        // Act
        var result = testData.Input.GrahamConvexHull();
        
        // Assert
        Assert.That(result.Count, Is.LessThan(3));
    }

    [Test]
    [TestCase("Square")]
    [TestCase("Diamond")]
    [TestCase("RectangleWithInteriorPoint")]
    [TestCase("Triangle")]
    [TestCase("Jagged")]
    public void GrahamConvexHull_ReturnsConvexHull(string testName)
    {
        // Arrange
        var testData = _hullTestData.Single(x => x.CaseName == testName);
        
        // Act
        var result = testData.Input.GrahamConvexHull();

        var resultPoints = result.Select(x => x.ToPoint3d()).Append(result[0].ToPoint3d()).ToList();
        var polylineResult = new PolylineCurve(resultPoints);

        // Preview
        PreviewHub.ResetDoc();

        new PolylineCurve(testData.Input.Select(x => x.ToPoint3d())).AddToPreviewDoc();
        polylineResult.Translate(0, 0, 0.1);
        polylineResult.PreviewViaHub(color: Color.DarkBlue);

        // Assert
        var area = AreaMassProperties.Compute(polylineResult).Area;

        Assert.That(result.Count, Is.EqualTo(testData.ExpectedCount));
        Assert.AreEqual(testData.ExpectedArea, area, 1e-3);

        // Verify convexity: all consecutive triples form a left turn (CCW)
        for (var i = 0; i < result.Count; i++)
        {
            var a = result[i];
            var b = result[(i + 1) % result.Count];
            var c = result[(i + 2) % result.Count];
            var cross = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);
            Assert.That(cross, Is.GreaterThan(-1e-12));
        }
    }

    private record DegenerateTestData(string CaseName, List<Point2d> Input);

    private static readonly List<DegenerateTestData> _degenerateTestData =
    [
        new(CaseName: "SinglePoint", Input: [new(1, 2)]),
        new(CaseName: "TwoPoints", Input: [new(0, 0), new(5, 0)]),
        new(CaseName: "CollinearPoints", Input: [new(0, 0), new(2, 0), new(5, 0)])
    ];

    private record HullTestData(string CaseName, List<Point2d> Input, int ExpectedCount, double ExpectedArea);

    private static readonly List<HullTestData> _hullTestData =
    [
        new(
            CaseName: "Square",
            Input: [new(0, 0), new(4, 0), new(4, 4), new(0, 4)],
            ExpectedCount: 4,
            ExpectedArea: 16),
        new(
            CaseName: "Diamond",
            Input: [new(1, 0), new(2, 1), new(1, 2), new(0, 1)],
            ExpectedCount: 4,
            ExpectedArea: 2),
        new(
            CaseName: "RectangleWithInteriorPoint",
            Input: [new(0, 0), new(6, 0), new(6, 4), new(0, 4), new(2, 2)],
            ExpectedCount: 4,
            ExpectedArea: 24),
        new(
            CaseName: "Triangle",
            Input: [new(0, 0), new(5, 0), new(2, 4)],
            ExpectedCount: 3,
            ExpectedArea: 10),
        new(
            CaseName: "Jagged",
            Input: [new(0, 0), new(5, 0), new(6,4), new(3,1), new(0,4)],
            ExpectedCount: 4,
            ExpectedArea: 22)
    ];
}
