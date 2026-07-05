using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.Urban.Core.Tests.ModelActions;

[RhinoTestFixture]
public class AddZoneStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("SingleZone")]
    [TestCase("MultipleZones")]
    [TestCase("EmptyList")]
    public void Run_AddsZonesToModel(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        IUrbanModelStep step = new AddZoneStep();

        // Act
        var result = step.Run(UrbanModel.Instance, new CreationDataCollection<ZoneData>(testData.Input));

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeZones();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Zones, Has.Count.EqualTo(testData.ExpectedCount));
        Assert.That(result, Has.Count.EqualTo(testData.ExpectedGeometryIdCount));

        if (testData.ExpectedCount > 0)
        {
            var allGeometryIds = UrbanModel.Instance.Zones
                .SelectMany(x => x.GetGeometryIds())
                .OrderBy(x => x)
                .ToList();
            var returnedIds = result.OrderBy(x => x).ToList();
            Assert.That(returnedIds, Is.EqualTo(allGeometryIds));
        }
    }

    private record AddZoneTestData(
        string CaseName,
        List<ZoneData> Input,
        int ExpectedCount,
        int ExpectedGeometryIdCount);

    private static readonly Polygon2D BigBoundary = new(
        new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(100, 0, 0),
            new Point3d(100, 100, 0),
            new Point3d(0, 100, 0),
            new Point3d(0, 0, 0),
        ]));

    private static readonly Polygon2D SmallBoundary = new(
        new PolylineCurve([
            new Point3d(10, 10, 0),
            new Point3d(30, 10, 0),
            new Point3d(30, 30, 0),
            new Point3d(10, 30, 0),
            new Point3d(10, 10, 0),
        ]));

    private static readonly List<AddZoneTestData> _testData =
    [
        new AddZoneTestData(
            CaseName: "SingleZone",
            Input: [new ZoneData(BigBoundary, "Downtown")],
            ExpectedCount: 1,
            ExpectedGeometryIdCount: 1),

        new AddZoneTestData(
            CaseName: "MultipleZones",
            Input:
            [
                new ZoneData(BigBoundary, "Downtown"),
                new ZoneData(SmallBoundary, "Park"),
            ],
            ExpectedCount: 2,
            ExpectedGeometryIdCount: 2),

        new AddZoneTestData(
            CaseName: "EmptyList",
            Input: [],
            ExpectedCount: 0,
            ExpectedGeometryIdCount: 0),
    ];
}
