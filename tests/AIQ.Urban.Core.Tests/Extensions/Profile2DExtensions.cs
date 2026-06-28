using System.Drawing;
using System.Linq;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Extensions;

[RhinoTestFixture]
public class Profile2DExtensionTests
{
    [Test]
    [TestCase("NoHoles")]
    [TestCase("OnlyInternalHoles")]
    [TestCase("OnlyPeripheralVoids")]
    [TestCase("MixedHoles")]
    public void WithoutPeripheralVoids_RemovesTouchingVoids(string testName)
    {
        // Arrange
        var testData = _peripheralVoidTestData.Single(x => x.CaseName == testName);

        // Act
        var result = testData.Input.WithoutPeripheralVoids();

        // Visualise
#if DEBUG
        VisualisePeripheralVoidTest(testData.Input, result);
#endif

        // Assert
        Assert.That(result, Is.Not.Null);
        var originalArea = AreaMassProperties.Compute(testData.Input.Geometry).Area;
        var resultArea = AreaMassProperties.Compute(result!.Geometry).Area;
        Assert.That(resultArea, Is.EqualTo(testData.ExpectedArea).Within(1e-4));

        if (testData.ExpectedHoleCount == 0)
        {
            Assert.That(result.Holes, Is.Empty);
            return;
        }

        Assert.That(result.Holes, Has.Count.EqualTo(testData.ExpectedHoleCount));
    }

    [Test]
    [TestCase("InsideLine")]
    [TestCase("OutsideLine")]
    [TestCase("CrossingHole")]
    [TestCase("LineOnHoleBoundary")]
    public void Contains_LineInProfile(string testName)
    {
        // Arrange
        var testData = _containsLineTestData.Single(x => x.CaseName == testName);

        // Act
        var result = testData.Profile.Contains(testData.Line);

        // Visualise
#if DEBUG
        VisualiseContainsTest(testData.Profile, testData.Line, result);
#endif

        // Assert
        Assert.That(result, Is.EqualTo(testData.Expected));
    }

    [Test]
    [TestCase("LineInsidePolygon")]
    [TestCase("LineOutsidePolygon")]
    [TestCase("LineCrossingBoundary")]
    [TestCase("LineOnEdge")]
    public void PolygonContains_Line(string testName)
    {
        // Arrange
        var testData = _polygonContainsLineTestData.Single(x => x.CaseName == testName);

        // Act
        var result = testData.Polygon.Contains(testData.Line);

        // Assert
        Assert.That(result, Is.EqualTo(testData.Expected));
    }

#if DEBUG
    private static void VisualisePeripheralVoidTest(Profile2D input, Profile2D result)
    {
        var doc = RhinoDoc.CreateHeadless(null);
        var inputAttribs = RhinoHelper.MakeAttributes(doc, "Input", Color.LightGray);
        var resultAttribs = RhinoHelper.MakeAttributes(doc, "Result", Color.Blue);

        doc.Objects.AddBrep(input.Geometry, inputAttribs);
        if (result is not null)
        {
            var copy = result.Geometry.DuplicateBrep();
            copy.Translate(0, 0, 0.5);
            doc.Objects.AddBrep(copy, resultAttribs);
        }
        Visualizer.View(doc);
    }

    private static void VisualiseContainsTest(Profile2D profile, Line line, bool contained)
    {
        var doc = RhinoDoc.CreateHeadless(null);
        var outlineAttribs = RhinoHelper.MakeAttributes(doc, "Profile", Color.Black);
        var lineAttribs = RhinoHelper.MakeAttributes(doc, "TestLine", contained ? Color.Green : Color.Orange);

        doc.Objects.AddBrep(profile.Geometry, outlineAttribs);
        doc.Objects.AddLine(line, lineAttribs);
        Visualizer.View(doc);
    }
#endif

    private record PeripheralVoidTestData(string CaseName, Profile2D Input, double ExpectedArea, int ExpectedHoleCount);

    private static readonly PeripheralVoidTestData[] _peripheralVoidTestData = BuildPeripheralVoidTestData();

    private static PeripheralVoidTestData[] BuildPeripheralVoidTestData()
    {
        var square10 = Profile2D.Create(
            Polygon2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]), [])!;
        var profileWithInternalHole = Profile2D.Create(
            Polygon2D.Create([(0, 0), (20, 0), (20, 10), (0, 10)]),
            [Polygon2D.Create([(5, 2), (15, 2), (15, 8), (5, 8)])])!;
        var profileWithPeripheralVoid = Profile2D.Create(
            Polygon2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]),
            [Polygon2D.Create([(0, 0), (5, 0), (5, 4), (0, 4)])])!;
        var profileWithBoth = Profile2D.Create(
            Polygon2D.Create([(0, 0), (20, 0), (20, 10), (0, 10)]),
            [
                Polygon2D.Create([(0, 0), (4, 0), (4, 3), (0, 3)]),
                Polygon2D.Create([(5, 2), (15, 2), (15, 8), (5, 8)]),
            ])!;

        return
        [
            new("NoHoles", square10, 100, 0),
            new("OnlyInternalHoles", profileWithInternalHole, 100, 1),
            new("OnlyPeripheralVoids", profileWithPeripheralVoid,
                AreaMassProperties.Compute(profileWithPeripheralVoid.Geometry).Area, 0),
            new("MixedHoles", profileWithBoth,
                AreaMassProperties.Compute(profileWithBoth.Geometry).Area, 1),
        ];
    }

    private record ContainsLineTestData(string CaseName, Profile2D Profile, Line Line, bool Expected);

    private static readonly ContainsLineTestData[] _containsLineTestData = BuildContainsLineTestData();

    private static ContainsLineTestData[] BuildContainsLineTestData()
    {
        var squareWithHole = Profile2D.Create(
            Polygon2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]),
            [Polygon2D.Create([(3, 3), (7, 3), (7, 7), (3, 7)])])!;

        return
        [
            new("InsideLine", squareWithHole,
                new Line(new Point3d(1, 5, 0), new Point3d(2, 5, 0)), true),
            new("OutsideLine", squareWithHole,
                new Line(new Point3d(20, 0, 0), new Point3d(30, 0, 0)), false),
            new("CrossingHole", squareWithHole,
                new Line(new Point3d(1, 5, 0), new Point3d(9, 5, 0)), false),
            new("LineOnHoleBoundary", squareWithHole,
                new Line(new Point3d(3, 4, 0), new Point3d(7, 4, 0)), true),
        ];
    }

    private record PolygonContainsLineTestData(string CaseName, Polygon2D Polygon, Line Line, bool Expected);

    private static readonly PolygonContainsLineTestData[] _polygonContainsLineTestData =
    [
        new("LineInsidePolygon",
            Polygon2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]),
            new Line(new Point3d(1, 5, 0), new Point3d(9, 5, 0)), true),
        new("LineOutsidePolygon",
            Polygon2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]),
            new Line(new Point3d(20, 0, 0), new Point3d(30, 0, 0)), false),
        new("LineCrossingBoundary",
            Polygon2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]),
            new Line(new Point3d(-5, 5, 0), new Point3d(15, 5, 0)), false),
        new("LineOnEdge",
            Polygon2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]),
            new Line(new Point3d(10, 2, 0), new Point3d(10, 8, 0)), false),
    ];
}