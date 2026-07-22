using System;
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

    private record PeripheralVoidTestData(string CaseName, Profile2D Input, double ExpectedArea, int ExpectedHoleCount);

    private static readonly PeripheralVoidTestData[] _peripheralVoidTestData = BuildPeripheralVoidTestData();

    private static PeripheralVoidTestData[] BuildPeripheralVoidTestData()
    {
        var mixedHolesInput = new Profile2D(
            Polycurve2D.Create([(0, 0), (20, 0), (20, 10), (0, 10)]),
            [
                Polycurve2D.Create([(0, 0), (4, 0), (4, 3), (0, 3)]),
                Polycurve2D.Create([(5, 2), (15, 2), (15, 8), (5, 8)]),
            ])!;

        return
        [
            new(CaseName: "NoHoles",
                Input: new Profile2D(
                    Polycurve2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]), [])!,
                ExpectedArea: 100,
                ExpectedHoleCount: 0),

            new(CaseName: "OnlyInternalHoles",
                Input: new Profile2D(
                    Polycurve2D.Create([(0, 0), (20, 0), (20, 10), (0, 10)]),
                    [Polycurve2D.Create([(5, 2), (15, 2), (15, 8), (5, 8)])])!,
                ExpectedArea: 140,
                ExpectedHoleCount: 1),

            new(CaseName: "OnlyPeripheralVoids",
                Input: new Profile2D(
                    Polycurve2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]),
                    [Polycurve2D.Create([(0, 0), (5, 0), (5, 4), (0, 4)])])!,
                ExpectedArea: 80,
                ExpectedHoleCount: 0),

            new(CaseName: "MixedHoles",
                Input: mixedHolesInput,
                ExpectedArea: 128,
                ExpectedHoleCount: 1),
        ];
    }


    [Test]
    [TestCase("InsideLine")]
    [TestCase("OutsideLine")]
    [TestCase("CrossingHole")]
    [TestCase("LineOnHoleBoundary")]
    public void Contains_LineInProfile(string testName)
    {
        // Arrange
        var td = GetContainsLineTestData(testName);
        var outline = Polycurve2D.Create(td.Outline.Select(v => (v[0], v[1])).ToList());
        var holes = td.Holes.Select(h => Polycurve2D.Create(h.Select(v => (v[0], v[1])).ToList())).ToList();
        var profile = new Profile2D(outline, holes)!;
        var line = new Line(new Point3d(td.LineFrom[0], td.LineFrom[1], 0),
                            new Point3d(td.LineTo[0], td.LineTo[1], 0));

        // Act
        var result = profile.Contains(line);

        // Visualise
#if DEBUG
        VisualiseContainsTest(profile, line, result);
#endif

        // Assert
        Assert.That(result, Is.EqualTo(td.Expected));
    }

#if DEBUG

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

  
    private record PrimitiveContainsLineTestData(
        string CaseName,
        double[][] Outline,
        double[][][] Holes,
        double[] LineFrom,
        double[] LineTo,
        bool Expected);

    private static PrimitiveContainsLineTestData GetContainsLineTestData(string caseName)
    {
        return caseName switch
        {
            "InsideLine" => new PrimitiveContainsLineTestData(
                CaseName: "InsideLine",
                Outline: [[0, 0], [10, 0], [10, 10], [0, 10]],
                Holes: [[[3, 3], [7, 3], [7, 7], [3, 7]]],
                LineFrom: [1, 5],
                LineTo: [2, 5],
                Expected: true),

            "OutsideLine" => new PrimitiveContainsLineTestData(
                CaseName: "OutsideLine",
                Outline: [[0, 0], [10, 0], [10, 10], [0, 10]],
                Holes: [[[3, 3], [7, 3], [7, 7], [3, 7]]],
                LineFrom: [20, 0],
                LineTo: [30, 0],
                Expected: false),

            "CrossingHole" => new PrimitiveContainsLineTestData(
                CaseName: "CrossingHole",
                Outline: [[0, 0], [10, 0], [10, 10], [0, 10]],
                Holes: [[[3, 3], [7, 3], [7, 7], [3, 7]]],
                LineFrom: [1, 5],
                LineTo: [9, 5],
                Expected: false),

            "LineOnHoleBoundary" => new PrimitiveContainsLineTestData(
                CaseName: "LineOnHoleBoundary",
                Outline: [[0, 0], [10, 0], [10, 10], [0, 10]],
                Holes: [[[3, 3], [7, 3], [7, 7], [3, 7]]],
                LineFrom: [3, 4],
                LineTo: [7, 4],
                Expected: false),

            _ => throw new ArgumentException($"Unknown test case: {caseName}")
        };
    }
}