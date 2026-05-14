using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
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
public class AddBuildingStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("SingleBuilding")]
    [TestCase("MultipleBuildings")]
    [TestCase("EmptyList")]
    public void Run_AddsBuildingsToModel(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        IUrbanModelStep step = new AddBuildingStep(testData.Input);

        // Act
        var result = step.Run(UrbanModel.Instance);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeBuildings();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(testData.ExpectedCount));
        Assert.That(result, Has.Count.EqualTo(testData.ExpectedGeometryIdCount));

        if (testData.ExpectedCount > 0)
        {
            var allGeometryIds = UrbanModel.Instance.Buildings
                .SelectMany(x => x.GetGeometryIds())
                .OrderBy(x => x)
                .ToList();
            var returnedIds = result.OrderBy(x => x).ToList();
            Assert.That(returnedIds, Is.EqualTo(allGeometryIds));
        }
    }

    private record AddBuildingTestData(
        string CaseName,
        List<BuildingData> Input,
        int ExpectedCount,
        int ExpectedGeometryIdCount);

    private static readonly Polygon2D SimpleFootprint = new(
        new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));

    private static readonly Polygon2D SmallFootprint = new(
        new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(5, 0, 0),
            new Point3d(5, 5, 0),
            new Point3d(0, 5, 0),
            new Point3d(0, 0, 0),
        ]));

    private static readonly List<AddBuildingTestData> _testData =
    [
        new AddBuildingTestData(
            CaseName: "SingleBuilding",
            Input: [new BuildingData(SimpleFootprint, 3.5, 5, DefaultUses.Residential)],
            ExpectedCount: 1,
            ExpectedGeometryIdCount: 6),

        new AddBuildingTestData(
            CaseName: "MultipleBuildings",
            Input:
            [
                new BuildingData(SimpleFootprint, 3.5, 5, DefaultUses.Residential),
                new BuildingData(SmallFootprint, 4.0, 3, DefaultUses.Commercial),
            ],
            ExpectedCount: 2,
            ExpectedGeometryIdCount: 10),

        new AddBuildingTestData(
            CaseName: "EmptyList",
            Input: [],
            ExpectedCount: 0,
            ExpectedGeometryIdCount: 0),
    ];
}
