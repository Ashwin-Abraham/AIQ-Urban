using System.Collections.Generic;
using System.Linq;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Extensions;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Extensions;

[RhinoTestFixture]
public class PolylineExtensionTests
{
    [Test]
    [TestCase("SinglePoint")]
    [TestCase("LineSegment")]
    [TestCase("CollinearPoints")]
    public void CreateMinimalBox_ReturnsNullForDegenerate(string testName)
    {
        var testData = _nullTestData.Single(x => x.CaseName == testName);
        var result = testData.Input.CreateMinimalBox();
        Assert.That(result, Is.False);
    }

    [Test]
    [TestCase("Square")]
    [TestCase("Diamond")]
    [TestCase("Rectangle")]
    [TestCase("Angled L")]
    public void CreateMinimalBox_ReturnsOrientedBox(string testName)
    {
        // Arrange
        var testData = _boxTestData.Single(x => x.CaseName == testName);

        // Act
        var result = testData.Input.CreateMinimalBox();

#if DEBUG
        // Preview
        PreviewHub.ResetDoc();
        new PolylineCurve(testData.Input).AddToPreviewDoc();
        new PolylineCurve(result).PreviewViaHub();
#endif
        
        // Assert
        var area = AreaMassProperties.Compute(result.ToPolylineCurve(), 1e-5).Area;
        Assert.That(area, Is.EqualTo(testData.ExpectedArea).Within(1e-6));
    }

    private record NullTestData(string CaseName, Polyline Input);

    private static readonly List<NullTestData> _nullTestData =
    [
        new(CaseName: "SinglePoint", Input: new Polyline { new(1, 2, 0) }),
        new(CaseName: "LineSegment", Input: new Polyline { new(0, 0, 0), new(5, 0, 0) }),
        new(CaseName: "CollinearPoints", Input: new Polyline { new(0, 0, 0), new(2, 0, 0), new(5, 0, 0) })
    ];

    private record BoxTestData(string CaseName, Polyline Input, double ExpectedArea);

    private static readonly List<BoxTestData> _boxTestData =
    [
        new(
            CaseName: "Square",
            Input: new Polyline { new(0, 0, 0), new(4, 0, 0), new(4, 4, 0), new(0, 4, 0) },
            ExpectedArea: 16),
        new(
            CaseName: "Diamond",
            Input: new Polyline { new(1, 0, 0), new(2, 1, 0), new(1, 2, 0), new(0, 1, 0) },
            ExpectedArea: 2),
        new(
            CaseName: "Rectangle",
            Input: new Polyline { new(0, 0, 0), new(6, 0, 0), new(6, 2, 0), new(0, 2, 0) },
            ExpectedArea: 12),
        new(
            CaseName: "Angled L",
            Input: new Polyline { new(0, 0, 0), new(7, 7, 0), new(6, 8, 0), new(0, 2, 0), new(-1,3,0), new(-2,2,0) },
            ExpectedArea: 28)
    ];
}
