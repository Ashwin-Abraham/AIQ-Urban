using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Tests.ModelActions.Tools;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.Urban.Core.Tests.ModelActions;

[RhinoTestFixture]
public class TransitSurfaceUnionerStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("SingleRoad")]
    [TestCase("TwoOverlappingRoads")]
    [TestCase("TwoSeparateRoads")]
    [TestCase("DifferentTransitTypes")]
    [TestCase("NoTransits")]
    public void Run_UnionsSurfacesByType(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        IUrbanModelStep addStep = new AddTransitStep(testData.TransitInputs);
        addStep.Run(UrbanModel.Instance);

        // Act
        IUrbanModelStep step = new TransitSurfaceUnioner();
        var result = step.Run(UrbanModel.Instance);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeTransitSurfaces();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.TransitSurfaces, Has.Count.EqualTo(testData.ExpectedSurfaceCount));
        Assert.That(result, Has.Count.EqualTo(testData.ExpectedSurfaceCount));

        foreach (var (profile, type) in UrbanModel.Instance.TransitSurfaces)
        {
            Assert.That(profile, Is.Not.Null);
            var area = AreaMassProperties.Compute(profile.Geometry)!.Area;
            Assert.That(area, Is.GreaterThan(0));
        }
    }

    private record TransitSurfaceTestData(
        string CaseName,
        List<TransitData> TransitInputs,
        int ExpectedSurfaceCount);

    private static Curve2D MakeCenterline(Point3d from, Point3d to)
        => new(new LineCurve(from, to), Guid.NewGuid());

    private static readonly List<TransitSurfaceTestData> _testData =
    [
        new(CaseName: "SingleRoad",
            TransitInputs: [new TransitData(MakeCenterline(new(0, 0, 0), new(30, 0, 0)), 8, TransitType.Road)],
            ExpectedSurfaceCount: 1),

        new(CaseName: "TwoOverlappingRoads",
            TransitInputs:
            [
                new TransitData(MakeCenterline(new(0, 0, 0), new(30, 0, 0)), 8, TransitType.Road),
                new TransitData(MakeCenterline(new(15, -15, 0), new(15, 15, 0)), 8, TransitType.Road),
            ],
            ExpectedSurfaceCount: 1),

        new(CaseName: "TwoSeparateRoads",
            TransitInputs:
            [
                new TransitData(MakeCenterline(new(0, 0, 0), new(20, 0, 0)), 6, TransitType.Road),
                new TransitData(MakeCenterline(new(0, 50, 0), new(20, 50, 0)), 6, TransitType.Road),
            ],
            ExpectedSurfaceCount: 2),

        new(CaseName: "DifferentTransitTypes",
            TransitInputs:
            [
                new TransitData(MakeCenterline(new(0, 0, 0), new(30, 0, 0)), 8, TransitType.Road),
                new TransitData(MakeCenterline(new(0, 15, 0), new(30, 15, 0)), 6, TransitType.Railway),
            ],
            ExpectedSurfaceCount: 2),

        new(CaseName: "NoTransits",
            TransitInputs: [],
            ExpectedSurfaceCount: 0),
    ];
}
