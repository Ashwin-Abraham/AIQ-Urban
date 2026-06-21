using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.ShapeSubdividers;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.ShapeSubdividers;

[RhinoTestFixture]
public class BisectingCrossSectionTests
{
    [Test]
    [TestCase("Rectangle")]
    [TestCase("Diamond")]
    [TestCase("Angled L")]
    [TestCase("Triangle")]
    public void Run_BisectsShape(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        var subdivider = new BisectingCrossSection();

        var result = subdivider.Run(testData.Input);

#if DEBUG
        PreviewDivision(testData.Input, result);
#endif

        Assert.That(result.Fragmenets, Has.Count.EqualTo(testData.ExpectedFragmentCount));
        Assert.That(result.Dividers, Has.Count.EqualTo(1));
        Assert.That(result.Dividers[0].Id, Is.Not.EqualTo(Guid.Empty));

        var originalArea = AreaMassProperties.Compute(testData.Input.Geometry).Area;
        var fragmentAreaSum = result.Fragmenets
            .Sum(f => AreaMassProperties.Compute(f.Geometry).Area);
        Assert.That(fragmentAreaSum, Is.EqualTo(originalArea).Within(1e-4));

        foreach (var fragment in result.Fragmenets)
        {
            Assert.That(fragment.Geometry, Is.Not.Null);
            Assert.That(fragment.Geometry.Faces.Count, Is.GreaterThan(0));
            Assert.That(fragment.Outline.Geometry.IsClosed, Is.True);
            Assert.That(AreaMassProperties.Compute(fragment.Geometry).Area, Is.GreaterThan(0));
            Assert.That(fragment.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(fragment.Holes, Has.Count.EqualTo(testData.ExpectedHolesPerFragment));
        }
    }

    private record BisectingTestData(
        string CaseName,
        Profile2D Input,
        int ExpectedFragmentCount,
        int ExpectedHolesPerFragment);

    private static readonly List<BisectingTestData> _testData = BuildTestData();

    private static List<BisectingTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "Rectangle", Input: MakeRectangle(), ExpectedFragmentCount: 2, ExpectedHolesPerFragment: 0),
            new(CaseName: "Diamond", Input: MakeDiamond(), ExpectedFragmentCount: 2, ExpectedHolesPerFragment: 0),
            new(CaseName: "Angled L", Input: MakeAngledL(), ExpectedFragmentCount: 2, ExpectedHolesPerFragment: 0),
            new(CaseName: "Triangle", Input: MakeTriangle(), ExpectedFragmentCount: 2, ExpectedHolesPerFragment: 0),
        ];
    }

    private static Profile2D MakeRectangle()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(6, 0, 0),
            new Point3d(6, 2, 0),
            new Point3d(0, 2, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(brep, poly, []);
    }

    private static Profile2D MakeDiamond()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(2, 0, 0),
            new Point3d(4, 2, 0),
            new Point3d(2, 4, 0),
            new Point3d(0, 2, 0),
            new Point3d(2, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(brep, poly, []);
    }

    private static Profile2D MakeAngledL()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(7, 7, 0),
            new Point3d(6, 8, 0),
            new Point3d(0, 2, 0),
            new Point3d(-1, 3, 0),
            new Point3d(-2, 2, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(brep, poly, []);
    }

    private static Profile2D MakeTriangle()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(brep, poly, []);
    }



#if DEBUG
    private static void PreviewDivision(Profile2D input, DivisionResult result)
    {
        var doc = RhinoDoc.CreateHeadless(null);
        var outlineAttribs = RhinoHelper.MakeAttributes(doc, "Original Outline", Color.Black);
        doc.Objects.AddCurve(input.Outline.Geometry, outlineAttribs);

        var fragmentColors = new[] { Color.Red, Color.Green, Color.Blue, Color.Orange, Color.Purple };
        for (var i = 0; i < result.Fragmenets.Count; i++)
        {
            var attribs = RhinoHelper.MakeAttributes(doc, $"Fragment {i}", fragmentColors[i % fragmentColors.Length]);
            doc.Objects.AddBrep(result.Fragmenets[i].Geometry, attribs);
        }

        var dividerAttribs = RhinoHelper.MakeAttributes(doc, "Divider", Color.Yellow);
        doc.Objects.AddCurve(result.Dividers[0].Geometry, dividerAttribs);

        Visualizer.View(doc);
    }
#endif
}
