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
    [TestCase("Box10m_Floor3m")]
    [TestCase("Box5m_Floor3m")]
    [TestCase("EmptyEnvelopeList")]
    [TestCase("LoftedBrep")]
    public void Run_AddsBuildingsToModel(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        IUrbanModelStep step = new AddBuildingStep();

        // Act
        var result = step.Run(UrbanModel.Instance, testData.Input.Cast<ICreationData>().ToList());

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeBuildings(viewLocal: true);
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(testData.ExpectedCount));
        Assert.That(result, Has.Count.EqualTo(testData.ExpectedGeometryIdCount));

        if (testData.ExpectedCount > 0)
        {
            var building = UrbanModel.Instance.Buildings[0];
            Assert.That(building.FloorCount, Is.EqualTo(testData.ExpectedFirstFloorCount));
            Assert.That(building.FloorPlates, Has.Count.EqualTo(testData.ExpectedFirstFloorPlateCount));

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
        List<ICreationData> Input,
        int ExpectedCount,
        int ExpectedGeometryIdCount,
        int ExpectedFirstFloorCount,
        int ExpectedFirstFloorPlateCount);

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

    private static Brep MakeLoftedBrep(double width, double depth, double height)
    {
        var baseCurve = new Polyline(
            [
                new Point3d(-width / 2, -depth / 2, 0),
                new Point3d(width / 2, -depth / 2, 0),
                new Point3d(width / 2, depth / 2, 0),
                new Point3d(-width / 2, depth / 2, 0),
            ]);

        var midCurve = baseCurve.Duplicate();
        midCurve.Transform(Transform.Translation(new Vector3d(width / 2, 0, height / 2)));

        var topCurve = baseCurve.Duplicate();
        midCurve.Transform(Transform.Translation(new Vector3d(0, 0, height)));

        return Brep.CreateFromLoft([baseCurve.ToPolylineCurve(), midCurve.ToPolylineCurve(), topCurve.ToPolylineCurve()], baseCurve.PointAt(0), topCurve.PointAt(0), LoftType.Normal, true)[0];
    }

    private static Brep MakeBoxBrep(double width, double depth, double height)
    {
        return new Box(Plane.WorldXY, new Interval(0, width), new Interval(0, depth), new Interval(0, height)).ToBrep();
    }

    private static readonly List<AddBuildingTestData> _testData =
    [
        new AddBuildingTestData(
            CaseName: "SingleBuilding",
            Input: [new BuildingFootprintData(SimpleFootprint, 3.5, 5, DefaultUses.Create("Residential"))],
            ExpectedCount: 1,
            ExpectedGeometryIdCount: 6,
            ExpectedFirstFloorCount: 5,
            ExpectedFirstFloorPlateCount: 5),

        new AddBuildingTestData(
            CaseName: "MultipleBuildings",
            Input:
            [
                new BuildingFootprintData(SimpleFootprint, 3.5, 5, DefaultUses.Create("Residential")),
                new BuildingFootprintData(SmallFootprint, 4.0, 3, DefaultUses.Create("Commercial")),
            ],
            ExpectedCount: 2,
            ExpectedGeometryIdCount: 10,
            ExpectedFirstFloorCount: 5,
            ExpectedFirstFloorPlateCount: 5),

        new AddBuildingTestData(
            CaseName: "EmptyList",
            Input: [],
            ExpectedCount: 0,
            ExpectedGeometryIdCount: 0,
            ExpectedFirstFloorCount: 0,
            ExpectedFirstFloorPlateCount: 0),

        new AddBuildingTestData(
            CaseName: "Box10m_Floor3m",
            Input:
            [
                new BuildingMassData(
                    new BrepElement(MakeBoxBrep(10, 10, 10), Guid.NewGuid()),
                    3.0,
                    DefaultUses.Create("Residential")),
            ],
            ExpectedCount: 1,
            ExpectedGeometryIdCount: 5,
            ExpectedFirstFloorCount: 3,
            ExpectedFirstFloorPlateCount: 4),

        new AddBuildingTestData(
            CaseName: "Box5m_Floor3m",
            Input:
            [
                new BuildingMassData(
                    new BrepElement(MakeBoxBrep(10, 10, 5), Guid.NewGuid()),
                    3.0,
                    DefaultUses.Create("Commercial")),
            ],
            ExpectedCount: 1,
            ExpectedGeometryIdCount: 3,
            ExpectedFirstFloorCount: 1,
            ExpectedFirstFloorPlateCount: 2),

        new AddBuildingTestData(
            CaseName: "EmptyEnvelopeList",
            Input: [],
            ExpectedCount: 0,
            ExpectedGeometryIdCount: 0,
            ExpectedFirstFloorCount: 0,
            ExpectedFirstFloorPlateCount: 0),

        new AddBuildingTestData(
            CaseName: "LoftedBrep",
            Input:
            [
                new BuildingMassData(
                    new BrepElement(MakeLoftedBrep(10, 3, 22), Guid.NewGuid()),
                    3.0,
                    DefaultUses.Create("Retail")),
            ],
            ExpectedCount: 1,
            ExpectedGeometryIdCount: 4,
            ExpectedFirstFloorCount: 2,
            ExpectedFirstFloorPlateCount: 3),
    ];
}
