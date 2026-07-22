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
using AIQ.PreviewHelper.Core;

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
        PreviewHub.ResetDoc();
        testData.Input.AddToPreviewDoc();
        result.Fragments.AddToPreviewDoc(Color.Blue);
        result.Dividers.AddToPreviewDoc(Color.DarkRed);
        PreviewHub.PreviewViaHub();
#endif

        Assert.That(result.Fragments, Has.Count.EqualTo(testData.ExpectedFragmentCount));
        Assert.That(result.Dividers, Has.Count.EqualTo(1));
        Assert.That(result.Dividers[0].Id, Is.Not.EqualTo(Guid.Empty));

        var originalArea = AreaMassProperties.Compute(testData.Input.Geometry).Area;
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
            new(
                CaseName: "Rectangle",
                Input: new Profile2D(
                    Polycurve2D.Rectangle(new(0, 0), new(6, 2)), [])!,
                ExpectedFragmentCount: 2,
                ExpectedHolesPerFragment: 0),
            new(
                CaseName: "Diamond",
                Input: new Profile2D(
                    Polycurve2D.Create([(2, 0), (4, 2), (2, 4), (0, 2)]), [])!,
                ExpectedFragmentCount: 2,
                ExpectedHolesPerFragment: 0),
            new(
                CaseName: "Angled L",
                Input: new Profile2D(
                    Polycurve2D.Create([(0, 0), (7, 7), (6, 8), (0, 2), (-1, 3), (-2, 2)]), [])!,
                ExpectedFragmentCount: 2,
                ExpectedHolesPerFragment: 0),
            new(
                CaseName: "Triangle",
                Input: new Profile2D(
                    Polycurve2D.Create([(0, 0), (10, 0), (0, 10)]), [])!,
                ExpectedFragmentCount: 2,
                ExpectedHolesPerFragment: 0),
        ];
    }
}
