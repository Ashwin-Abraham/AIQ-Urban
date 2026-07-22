using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.ShapeSubdividers;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.ShapeSubdividers;

[RhinoTestFixture]
public class LongEdgeToCenterTests
{
    [Test]
    [TestCase("Square_DividerCount2")]
    [TestCase("Rectangle_DividerCount2")]
    [TestCase("Triangle_DividerCount2")]
    [TestCase("Rectangle_DividerCount0")]
    [TestCase("Rectangle_DividerCount1")]
    [TestCase("Triangle_DividerCount3")]
    public void Run_ProducesExpectedFragments(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        var subdivider = new LongEdgeToCenter(testData.DividerCount);

        // Act
        var result = subdivider.Run(testData.Input);

        // Visualise
#if DEBUG
        PreviewDivision(testData.Input, result);
#endif

        // Assert
        var segmentCount = testData.Input.Outline.Geometry.DuplicateSegments().Length;
        var expectedDividerCount = Math.Min(testData.DividerCount, segmentCount);

        Assert.That(result.Fragments, Has.Count.EqualTo(testData.ExpectedFragmentCount));
        Assert.That(result.Dividers, Has.Count.EqualTo(expectedDividerCount));
        Assert.That(result.Dividers.Select(x => x.Id), Is.Unique);
        Assert.That(result.Dividers.Select(x => x.Id), Has.All.Not.EqualTo(Guid.Empty));

        var originalArea = AreaMassProperties.Compute(testData.Input.Outline.Geometry).Area;
        var fragmentAreaSum = result.Fragments
            .Sum(f => AreaMassProperties.Compute(f.Geometry).Area);
        Assert.That(fragmentAreaSum, Is.EqualTo(originalArea).Within(1e-4));

        foreach (var fragment in result.Fragments)
        {
            Assert.That(fragment.Geometry, Is.Not.Null);
            Assert.That(fragment.Geometry.Faces.Count, Is.GreaterThan(0));
            Assert.That(fragment.Outline.Geometry.IsClosed, Is.True);
            Assert.That(AreaMassProperties.Compute(fragment.Geometry).Area, Is.GreaterThan(0));
            Assert.That(fragment.Id, Is.Not.EqualTo(Guid.Empty));
        }
    }

    private record LongEdgeTestData(
        string CaseName,
        Profile2D Input,
        int DividerCount,
        int ExpectedFragmentCount);

    private static readonly List<LongEdgeTestData> _testData = BuildTestData();

    private static List<LongEdgeTestData> BuildTestData()
    {
        return
        [
            new(
                CaseName: "Square_DividerCount2",
                Input: MakeSquare(),
                DividerCount: 2,
                ExpectedFragmentCount: 2),

            new(
                CaseName: "Rectangle_DividerCount2",
                Input: MakeRectangle(20, 10),
                DividerCount: 2,
                ExpectedFragmentCount: 2),

            new(
                CaseName: "Triangle_DividerCount2",
                Input: MakeTriangle(),
                DividerCount: 2,
                ExpectedFragmentCount: 2),

            new(
                CaseName: "Rectangle_DividerCount0",
                Input: MakeRectangle(20, 10),
                DividerCount: 0,
                ExpectedFragmentCount: 1),

            new(
                CaseName: "Rectangle_DividerCount1",
                Input: MakeRectangle(20, 10),
                DividerCount: 0,
                ExpectedFragmentCount: 1),

            new(
                CaseName: "Triangle_DividerCount3",
                Input: MakeTriangle(),
                DividerCount: 3,
                ExpectedFragmentCount: 3),
        ];
    }

    private static Profile2D MakeSquare()
    {
        var poly = new Polycurve2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        return new Profile2D(poly, []);
    }

    private static Profile2D MakeRectangle(double w, double h)
    {
        var poly = new Polycurve2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(w, 0, 0),
            new Point3d(w, h, 0),
            new Point3d(0, h, 0),
            new Point3d(0, 0, 0),
        ]));
        return new Profile2D(poly, []);
    }

    private static Profile2D MakeTriangle()
    {
        var poly = new Polycurve2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(5, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        return new Profile2D(poly, []);
    }

#if DEBUG
    private static void PreviewDivision(Profile2D input, DivisionResult result)
    {
        var doc = RhinoDoc.CreateHeadless(null);
        var outlineAttribs = RhinoHelper.MakeAttributes(doc, "Original Outline", Color.Black);
        doc.Objects.AddCurve(input.Outline.Geometry, outlineAttribs);

        var fragmentColors = new[] { Color.Red, Color.Green, Color.Blue, Color.Orange, Color.Purple };
        for (var i = 0; i < result.Fragments.Count; i++)
        {
            var attribs = RhinoHelper.MakeAttributes(doc, $"Fragment {i}", fragmentColors[i % fragmentColors.Length]);
            doc.Objects.AddBrep(result.Fragments[i].Geometry, attribs);
        }

        var dividerAttribs = RhinoHelper.MakeAttributes(doc, "Dividers", Color.Yellow);
        foreach (var d in result.Dividers)
            doc.Objects.AddCurve(d.Geometry, dividerAttribs);

        Visualizer.View(doc);
    }
#endif
}
