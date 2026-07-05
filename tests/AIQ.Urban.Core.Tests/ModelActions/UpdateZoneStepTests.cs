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
public class UpdateZoneStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("UpdateExisting")]
    [TestCase("UpdateNonexistent")]
    public void Run_UpdatesZone(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange: add initial zones and capture their actual geometry IDs
        IUrbanModelStep addStep = new AddZoneStep();
        var addedIds = addStep.Run(UrbanModel.Instance, new CreationDataCollection<ZoneData>(testData.InitialZones));

        var oldBoundaryId = addedIds[0];
        var oldCount = UrbanModel.Instance.Zones.Count;
        var oldName = UrbanModel.Instance.Zones[0].Name;

        // Act
        var updateStep = new UpdateZoneStep(
            testData.UseExistingId ? oldBoundaryId : Guid.NewGuid(),
            testData.NewBoundary);
        var result = updateStep.Run(UrbanModel.Instance);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeZones();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Zones, Has.Count.EqualTo(testData.ExpectedZoneCount));

        if (testData.UseExistingId)
        {
            // The old boundary ID should no longer be present
            var allCurrentIds = UrbanModel.Instance.Zones
                .SelectMany(z => z.GetGeometryIds())
                .ToHashSet();
            Assert.That(allCurrentIds, Does.Not.Contain(oldBoundaryId));

            var allResultIds = result.AddedIds.Concat(result.DeletedIds).ToList();
            Assert.That(result.AddedIds, Has.Count.EqualTo(1));
            Assert.That(result.DeletedIds, Has.Count.EqualTo(1));
            Assert.That(allResultIds, Has.All.Not.EqualTo(Guid.Empty));
            Assert.That(allResultIds, Is.Unique);

            // The new zone should have the new boundary geometry
            var updatedZone = UrbanModel.Instance.Zones[0];
            Assert.That(updatedZone.Name, Is.EqualTo(oldName));
            Assert.That(updatedZone.Boundary.Id, Is.Not.EqualTo(oldBoundaryId));
        }
        else
        {
            // Non-existent ID: no changes
            Assert.That(result.AddedIds, Is.Empty);
            Assert.That(result.DeletedIds, Is.Empty);
            Assert.That(UrbanModel.Instance.Zones, Has.Count.EqualTo(oldCount));
        }
    }

    private record UpdateZoneTestData(
        string CaseName,
        List<ZoneData> InitialZones,
        bool UseExistingId,
        Curve NewBoundary,
        int ExpectedZoneCount);

    private static Polygon2D MakeBoundary(double x, double y)
    {
        return new Polygon2D(new PolylineCurve([
            new Point3d(x, y, 0),
            new Point3d(x + 10, y, 0),
            new Point3d(x + 10, y + 10, 0),
            new Point3d(x, y + 10, 0),
            new Point3d(x, y, 0),
        ]));
    }

    private static readonly List<UpdateZoneTestData> _testData =
    [
        new UpdateZoneTestData(
            CaseName: "UpdateExisting",
            InitialZones:
            [
                new ZoneData(MakeBoundary(0, 0), "TestZone"),
            ],
            UseExistingId: true,
            NewBoundary: new PolylineCurve([
                new Point3d(5, 5, 0),
                new Point3d(15, 5, 0),
                new Point3d(15, 15, 0),
                new Point3d(5, 15, 0),
                new Point3d(5, 5, 0),
            ]),
            ExpectedZoneCount: 1),

        new UpdateZoneTestData(
            CaseName: "UpdateNonexistent",
            InitialZones:
            [
                new ZoneData(MakeBoundary(0, 0), "TestZone"),
            ],
            UseExistingId: false,
            NewBoundary: new PolylineCurve([
                new Point3d(5, 5, 0),
                new Point3d(15, 5, 0),
                new Point3d(15, 15, 0),
                new Point3d(5, 15, 0),
                new Point3d(5, 5, 0),
            ]),
            ExpectedZoneCount: 1),
    ];
}
