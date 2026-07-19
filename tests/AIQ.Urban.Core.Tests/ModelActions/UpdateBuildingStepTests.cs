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
public class UpdateBuildingStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("UpdateExisting")]
    [TestCase("UpdateNonexistent")]
    public void Run_UpdatesBuilding(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange: add initial building via mass-mode constructor
        IUrbanModelStep addStep = new AddBuildingStep();
        var addedIds = addStep.Run(UrbanModel.Instance, testData.InitialBuildings.Cast<ICreationData>().ToList());

        var oldEnvelopeId = UrbanModel.Instance.Buildings[0].Envelope.Id;
        var oldFloorPlateCount = UrbanModel.Instance.Buildings[0].FloorPlates.Count;
        var oldPlateUses = UrbanModel.Instance.Buildings[0].FloorPlates
            .OrderBy(p => p.Outline.Geometry.PointAtStart.Z)
            .Select(p => p.Use.Name)
            .ToList();
        var oldCount = UrbanModel.Instance.Buildings.Count;

        // Act
        var updateStep = new UpdateBuildingStep();
        var result = updateStep.Run(UrbanModel.Instance, [new BuildingUpdateData(
            testData.UseExistingId ? oldEnvelopeId : Guid.NewGuid(),
            testData.NewEnvelope,
            testData.TypicalFloorHeight,
            UrbanModel.Instance.Buildings[0].FloorPlates
                .OrderBy(p => p.Outline.Geometry.PointAtStart.Z)
                .Select(p => p.Use)
                .ToList())]);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeBuildings(viewLocal: true);
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(testData.ExpectedBuildingCount));

        if (testData.UseExistingId)
        {
            var newBuilding = UrbanModel.Instance.Buildings[0];

            // Old envelope ID should be gone
            var allCurrentIds = UrbanModel.Instance.Buildings
                .SelectMany(b => b.GetGeometryIds())
                .ToHashSet();
            Assert.That(allCurrentIds, Does.Not.Contain(oldEnvelopeId));

            // Result IDs are valid
            Assert.That(result.AddedIds, Has.Count.GreaterThan(0));
            Assert.That(result.DeletedIds, Has.Count.GreaterThan(0));
            Assert.That(result.AddedIds, Has.All.Not.EqualTo(Guid.Empty));
            Assert.That(result.DeletedIds, Has.All.Not.EqualTo(Guid.Empty));

            // New building has correct floor count from new envelope
            Assert.That(newBuilding.FloorPlates.Count, Is.EqualTo(testData.ExpectedNewFloorPlateCount));

            // Old uses are preserved (mapped by Z-level)
            var newPlateUses = newBuilding.FloorPlates
                .OrderBy(p => p.Outline.Geometry.PointAtStart.Z)
                .Select(p => p.Use.Name)
                .ToList();
            Assert.That(newPlateUses, Is.EqualTo(testData.ExpectedPreservedUses));
        }
        else
        {
            // Non-existent ID: no changes
            Assert.That(result.AddedIds, Is.Empty);
            Assert.That(result.DeletedIds, Is.Empty);
            Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(oldCount));
        }
    }

    private record UpdateBuildingTestData(
        string CaseName,
        List<ICreationData> InitialBuildings,
        bool UseExistingId,
        Brep NewEnvelope,
        double TypicalFloorHeight,
        int ExpectedBuildingCount,
        int ExpectedNewFloorPlateCount,
        List<string> ExpectedPreservedUses);

    private static Brep MakeBoxBrep(double width, double depth, double height)
    {
        return new Box(Plane.WorldXY, new Interval(0, width), new Interval(0, depth), new Interval(0, height)).ToBrep();
    }

    private static readonly List<UpdateBuildingTestData> _testData = BuildTestData();

    private static List<UpdateBuildingTestData> BuildTestData()
    {
        // 10x10x9m box, 3m floor height → 3 floors, all Residential
        var initialEnvelope = MakeBoxBrep(10, 10, 9);
        var initialUse = DefaultUses.Create("Residential");

        // Taller envelope: 10x10x15m, 3m floor height → 5 floors
        var tallerEnvelope = MakeBoxBrep(10, 10, 15);

        return
        [
            new UpdateBuildingTestData(
                CaseName: "UpdateExisting",
                InitialBuildings:
                [
                    new BuildingMassData(new BrepElement(initialEnvelope, Guid.NewGuid()), 3.0, initialUse),
                ],
                UseExistingId: true,
                NewEnvelope: tallerEnvelope,
                TypicalFloorHeight: 3.0,
                ExpectedBuildingCount: 1,
                ExpectedNewFloorPlateCount: 5,
                ExpectedPreservedUses: ["Residential", "Residential", "Residential", "Residential", "Residential"]),

            new UpdateBuildingTestData(
                CaseName: "UpdateNonexistent",
                InitialBuildings:
                [
                    new BuildingMassData(new BrepElement(initialEnvelope, Guid.NewGuid()), 3.0, initialUse),
                ],
                UseExistingId: false,
                NewEnvelope: tallerEnvelope,
                TypicalFloorHeight: 3.0,
                ExpectedBuildingCount: 1,
                ExpectedNewFloorPlateCount: 0,
                ExpectedPreservedUses: []),
        ];
    }
}
