using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.Urban.Core.Tests.ModelActions;

[RhinoTestFixture]
public class ChangeBuildingUseStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("BuildingWide")]
    [TestCase("PerPlate")]
    [TestCase("BuildingWins")]
    [TestCase("Mixed")]
    [TestCase("MultipleBuildings")]
    [TestCase("NoMatch")]
    [TestCase("UnmatchedGuid")]
    public void Run_ChangesBuildingUse(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        foreach (var building in testData.InitialBuildings)
            UrbanModel.Instance.Buildings.Add(building);

        IUrbanModelStep step = new ChangeBuildingUseStep(
            testData.GeometryIds,
            testData.NewUse);

        // Act
        var result = step.Run(UrbanModel.Instance);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeBuildings(viewLocal: true);
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(testData.InitialBuildings.Count));

        for (int i = 0; i < UrbanModel.Instance.Buildings.Count; i++)
        {
            var building = UrbanModel.Instance.Buildings[i];
            var expectedPlates = testData.ExpectedPlateUses[i];

            Assert.That(building.FloorPlates, Has.Count.EqualTo(expectedPlates.Count));
            for (int j = 0; j < expectedPlates.Count; j++)
                Assert.That(building.FloorPlates[j].Use, Is.EqualTo(expectedPlates[j]));
        }

        Assert.That(result, Has.Count.EqualTo(testData.ExpectedResultCount));
    }

    private record ChangeBuildingUseTestData(
        string CaseName,
        List<Building> InitialBuildings,
        List<Guid> GeometryIds,
        Use NewUse,
        List<List<Use>> ExpectedPlateUses,
        int ExpectedResultCount);

    private static Polygon2D MakeOutline(double size, Point3d origin = default)
    {
        return new Polygon2D(new PolylineCurve([
            new Point3d(origin.X, origin.Y, 0),
            new Point3d(origin.X + size, origin.Y, 0),
            new Point3d(origin.X + size, origin.Y + size, 0),
            new Point3d(origin.X, origin.Y + size, 0),
            new Point3d(origin.X, origin.Y, 0),
        ]));
    }

    private static Building MakeBuilding(double size, double floorHeight, int floorCount, Use use, Point3d origin = default)
    {
        return new Building(MakeOutline(size, origin), floorHeight, floorCount, use);
    }

    private static readonly List<ChangeBuildingUseTestData> _testData = BuildTestData();

    private static List<ChangeBuildingUseTestData> BuildTestData()
    {
        var bwB1 = MakeBuilding(10, 3.0, 3, DefaultUses.Public);
        var ppB1 = MakeBuilding(10, 3.0, 3, DefaultUses.Public);
        var bwB2 = MakeBuilding(8, 3.0, 2, DefaultUses.Commercial);

        var buildingWinsB = MakeBuilding(10, 3.0, 3, DefaultUses.Public);

        var mixedB1 = MakeBuilding(10, 3.0, 3, DefaultUses.Public);
        var mixedB2 = MakeBuilding(8, 3.0, 2, DefaultUses.Commercial, new(25,0,0));

        var multiB1 = MakeBuilding(10, 3.0, 3, DefaultUses.Public);
        var multiB2 = MakeBuilding(8, 3.0, 2, DefaultUses.Public, new(25, 0, 0));

        var noMatchB = MakeBuilding(10, 3.0, 3, DefaultUses.Public);

        var unmatchedB = MakeBuilding(10, 3.0, 3, DefaultUses.Public);

        var residential = DefaultUses.Residential;
        var retail = DefaultUses.Retail;

        return
        [
            new ChangeBuildingUseTestData(
                "BuildingWide",
                InitialBuildings: [bwB1, bwB2],
                GeometryIds: [bwB1.Envelope.Id],
                NewUse: residential,
                ExpectedPlateUses:
                [
                    [residential, residential, residential], // bwB1 all plates change
                    [DefaultUses.Commercial, DefaultUses.Commercial], // bwB2 unchanged
                ],
                ExpectedResultCount: 8), // 1 envelope + 3 plates (old) + 1 envelope + 3 plates (new)

            new ChangeBuildingUseTestData(
                "PerPlate",
                InitialBuildings: [ppB1],
                GeometryIds: [ppB1.FloorPlates[0].Outline.Id],
                NewUse: retail,
                ExpectedPlateUses:
                [
                    [retail, DefaultUses.Public, DefaultUses.Public], // only plate 0 changes
                ],
                ExpectedResultCount: 2), // 1 plate outline old + 1 new

            new ChangeBuildingUseTestData(
                "BuildingWins",
                InitialBuildings: [buildingWinsB],
                GeometryIds:
                [
                    buildingWinsB.Envelope.Id,                         // building-wide
                    buildingWinsB.FloorPlates[1].Outline.Id,           // per-plate (should be ignored)
                ],
                NewUse: residential,
                ExpectedPlateUses:
                [
                    [residential, residential, residential], // envelope wins, all plates change
                ],
                ExpectedResultCount: 8), // 1 envelope + 3 plates (old) + 1 envelope + 3 plates (new)

            new ChangeBuildingUseTestData(
                "Mixed",
                InitialBuildings: [mixedB1, mixedB2],
                GeometryIds:
                [
                    mixedB1.Envelope.Id,                       // B1 building-wide
                    mixedB2.FloorPlates[0].Outline.Id,         // B2 per-plate
                ],
                NewUse: residential,
                ExpectedPlateUses:
                [
                    [residential, residential, residential], // B1 all plates
                    [residential, DefaultUses.Commercial],   // B2 only plate 0
                ],
                ExpectedResultCount: 10), // B1: 1+3 old + 1+3 new = 8; B2: 1 old + 1 new = 2; total = 10

            new ChangeBuildingUseTestData(
                "MultipleBuildings",
                InitialBuildings: [multiB1, multiB2],
                GeometryIds: [multiB1.Envelope.Id, multiB2.Envelope.Id],
                NewUse: residential,
                ExpectedPlateUses:
                [
                    [residential, residential, residential],
                    [residential, residential],
                ],
                ExpectedResultCount: 14), // B1: 4 old + 4 new = 8; B2: 3 old + 3 new = 6; total = 14

            new ChangeBuildingUseTestData(
                "NoMatch",
                InitialBuildings: [noMatchB],
                GeometryIds: [],
                NewUse: residential,
                ExpectedPlateUses:
                [
                    [DefaultUses.Public, DefaultUses.Public, DefaultUses.Public], // unchanged
                ],
                ExpectedResultCount: 0),

            new ChangeBuildingUseTestData(
                "UnmatchedGuid",
                InitialBuildings: [unmatchedB],
                GeometryIds: [Guid.NewGuid(), Guid.NewGuid(), unmatchedB.Envelope.Id],
                NewUse: residential,
                ExpectedPlateUses:
                [
                    [residential, residential, residential], // matched by envelope, unmatched GUIDs ignored
                ],
                ExpectedResultCount: 8), // 1 envelope + 3 plates (old) + 1 envelope + 3 plates (new)
        ];
    }
}
