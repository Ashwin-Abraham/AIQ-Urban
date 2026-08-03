using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.Urban.Core.Tests.ModelActions;

[RhinoTestFixture]
public class ReplaceFloorPlateStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("HappyPath")]
    [TestCase("RejectExistingZ")]
    [TestCase("RejectOutOfBounds")]
    public void Run_ReplaceFloorPlate(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        var building = new Building(testData.Envelope, 3.0, testData.Use);
        UrbanModel.Instance.AddBuilding(building);

        var targetPlate = building.FloorPlates[testData.TargetPlateIndex];
        var oldPlateCount = building.FloorPlates.Count;
        var oldTypicalFloorHeight = building.TypicalFloorHeight;

        // Act
        var step = new ReplaceFloorPlateStep();
        var result = step.Run(UrbanModel.Instance, [new FloorPlateReplaceData(targetPlate, testData.NewZ)]);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeBuildings(viewLocal: true);
#endif

        // Assert
        if (testData.ExpectedReplaced)
        {
            Assert.That(result.AddedIds, Has.Count.EqualTo(1));
            Assert.That(result.DeletedIds, Has.Count.EqualTo(1));

            var newPlate = building.FloorPlates.Single(p => p.Id == result.AddedIds[0]);
            Assert.That(newPlate.Parent, Is.SameAs(building));
            Assert.That(newPlate.Use.Name, Is.EqualTo(testData.Use.Name));
            Assert.That(newPlate.Z(), Is.EqualTo(testData.NewZ).Within(1e-3));
            Assert.That(building.FloorPlates, Has.Count.EqualTo(oldPlateCount));
            Assert.That(building.TypicalFloorHeight, Is.EqualTo(testData.ExpectedTypicalFloorHeight).Within(1e-3));
        }
        else
        {
            Assert.That(result.AddedIds, Is.Empty);
            Assert.That(result.DeletedIds, Is.Empty);
            Assert.That(building.FloorPlates, Has.Count.EqualTo(oldPlateCount));
            Assert.That(building.TypicalFloorHeight, Is.EqualTo(oldTypicalFloorHeight).Within(1e-3));
        }
    }

    private record ReplaceFloorPlateTestData(
        string CaseName,
        Brep Envelope,
        Use Use,
        int TargetPlateIndex,
        double NewZ,
        bool ExpectedReplaced,
        double ExpectedTypicalFloorHeight);

    private static Brep MakeBoxBrep(double width, double depth, double height)
    {
        return new Box(Plane.WorldXY, new Interval(0, width), new Interval(0, depth), new Interval(0, height)).ToBrep();
    }

    private static readonly List<ReplaceFloorPlateTestData> _testData = BuildTestData();

    private static List<ReplaceFloorPlateTestData> BuildTestData()
    {
        // 10x10x9m box, 3m floor height → 3 floors
        var envelope = MakeBoxBrep(10, 10, 9);
        var use = DefaultUses.Create("Residential");

        return
        [
            // Move middle plate (index 1, Z≈3) up to Z=4.5
            new ReplaceFloorPlateTestData(
                CaseName: "HappyPath",
                Envelope: envelope,
                Use: use,
                TargetPlateIndex: 1,
                NewZ: 4.5,
                ExpectedReplaced: true,
                ExpectedTypicalFloorHeight: 4.5),

            // Try to move middle plate to Z=6 (occupied by index 2)
            new ReplaceFloorPlateTestData(
                CaseName: "RejectExistingZ",
                Envelope: envelope,
                Use: use,
                TargetPlateIndex: 1,
                NewZ: 6.0,
                ExpectedReplaced: false,
                ExpectedTypicalFloorHeight: 3.0),

            // Try to move top plate (index 2, Z≈6) to Z=20 (above envelope)
            new ReplaceFloorPlateTestData(
                CaseName: "RejectOutOfBounds",
                Envelope: envelope,
                Use: use,
                TargetPlateIndex: 2,
                NewZ: 20.0,
                ExpectedReplaced: false,
                ExpectedTypicalFloorHeight: 3.0),
        ];
    }
}
