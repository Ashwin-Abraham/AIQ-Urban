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
        var input = BuildInput(testData.CaseName);
        var result = step.Run(UrbanModel.Instance, input);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeBuildings(viewLocal: true);
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(testData.ExpectedCount));
        Assert.That(result.AddedIds, Has.Count.EqualTo(testData.ExpectedGeometryIdCount));

        if (testData.ExpectedCount > 0)
        {
            var building = UrbanModel.Instance.Buildings[0];
            Assert.That(building.FloorCount, Is.EqualTo(testData.ExpectedFirstFloorCount));
            Assert.That(building.FloorPlates, Has.Count.EqualTo(testData.ExpectedFirstFloorPlateCount));

            var allGeometryIds = UrbanModel.Instance.Buildings
                .SelectMany(x => x.GetGeometryIds())
                .OrderBy(x => x)
                .ToList();
            var returnedIds = result.AddedIds.OrderBy(x => x).ToList();
            Assert.That(returnedIds, Is.EqualTo(allGeometryIds));
        }
    }

    private record AddBuildingTestData(
        string CaseName,
        int ExpectedCount,
        int ExpectedGeometryIdCount,
        int ExpectedFirstFloorCount,
        int ExpectedFirstFloorPlateCount);

    private static Polygon2D MakeFootprint(double size) => new(new PolylineCurve([
        new Point3d(0, 0, 0),
        new Point3d(size, 0, 0),
        new Point3d(size, size, 0),
        new Point3d(0, size, 0),
        new Point3d(0, 0, 0),
    ]));

    private static Brep MakeLoftedBrep(double width, double depth, double height)
    {
        double topScale = 0.5;
        var tw = width * topScale / 2;
        var td = depth * topScale / 2;

        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);
        mesh.Vertices.Add(width, 0, 0);
        mesh.Vertices.Add(width, depth, 0);
        mesh.Vertices.Add(0, depth, 0);
        mesh.Vertices.Add(width / 2 - tw, depth / 2 - td, height);
        mesh.Vertices.Add(width / 2 + tw, depth / 2 - td, height);
        mesh.Vertices.Add(width / 2 + tw, depth / 2 + td, height);
        mesh.Vertices.Add(width / 2 - tw, depth / 2 + td, height);

        mesh.Faces.AddFace(0, 1, 2, 3);
        mesh.Faces.AddFace(4, 7, 6, 5);
        mesh.Faces.AddFace(0, 3, 7, 4);
        mesh.Faces.AddFace(1, 5, 6, 2);
        mesh.Faces.AddFace(0, 4, 5, 1);
        mesh.Faces.AddFace(3, 2, 6, 7);

        mesh.Normals.ComputeNormals();
        mesh.Compact();
        return Brep.CreateFromMesh(mesh, false);
    }

    private static Brep MakeBoxBrep(double width, double depth, double height)
    {
        return new Box(Plane.WorldXY, new Interval(0, width), new Interval(0, depth), new Interval(0, height)).ToBrep();
    }

    private static List<ICreationData> BuildInput(string caseName) => caseName switch
    {
        "SingleBuilding" =>
        [
            new BuildingFootprintData(MakeFootprint(10), 3.5, 5, DefaultUses.Create("Residential")),
        ],
        "MultipleBuildings" =>
        [
            new BuildingFootprintData(MakeFootprint(10), 3.5, 5, DefaultUses.Create("Residential")),
            new BuildingFootprintData(MakeFootprint(5), 4.0, 3, DefaultUses.Create("Commercial")),
        ],
        "Box10m_Floor3m" =>
        [
            new BuildingMassData(new BrepElement(MakeBoxBrep(10, 10, 10), Guid.NewGuid()), 3.0, DefaultUses.Create("Residential")),
        ],
        "Box5m_Floor3m" =>
        [
            new BuildingMassData(new BrepElement(MakeBoxBrep(10, 10, 5), Guid.NewGuid()), 3.0, DefaultUses.Create("Commercial")),
        ],
        "LoftedBrep" =>
        [
            new BuildingMassData(new BrepElement(MakeLoftedBrep(10, 3, 22), Guid.NewGuid()), 3.0, DefaultUses.Create("Retail")),
        ],
        _ => [],
    };

    private static readonly List<AddBuildingTestData> _testData =
    [
        new AddBuildingTestData(
            CaseName: "SingleBuilding",
            ExpectedCount: 1,
            ExpectedGeometryIdCount: 6,
            ExpectedFirstFloorCount: 5,
            ExpectedFirstFloorPlateCount: 5),

        new AddBuildingTestData(
            CaseName: "MultipleBuildings",
            ExpectedCount: 2,
            ExpectedGeometryIdCount: 10,
            ExpectedFirstFloorCount: 5,
            ExpectedFirstFloorPlateCount: 5),

        new AddBuildingTestData(
            CaseName: "EmptyList",
            ExpectedCount: 0,
            ExpectedGeometryIdCount: 0,
            ExpectedFirstFloorCount: 0,
            ExpectedFirstFloorPlateCount: 0),

        new AddBuildingTestData(
            CaseName: "Box10m_Floor3m",
            ExpectedCount: 1,
            ExpectedGeometryIdCount: 5,
            ExpectedFirstFloorCount: 3,
            ExpectedFirstFloorPlateCount: 4),

        new AddBuildingTestData(
            CaseName: "Box5m_Floor3m",
            ExpectedCount: 1,
            ExpectedGeometryIdCount: 3,
            ExpectedFirstFloorCount: 1,
            ExpectedFirstFloorPlateCount: 2),

        new AddBuildingTestData(
            CaseName: "EmptyEnvelopeList",
            ExpectedCount: 0,
            ExpectedGeometryIdCount: 0,
            ExpectedFirstFloorCount: 0,
            ExpectedFirstFloorPlateCount: 0),

        new AddBuildingTestData(
            CaseName: "LoftedBrep",
            ExpectedCount: 1,
            ExpectedGeometryIdCount: 9,
            ExpectedFirstFloorCount: 7,
            ExpectedFirstFloorPlateCount: 8),
    ];
}
