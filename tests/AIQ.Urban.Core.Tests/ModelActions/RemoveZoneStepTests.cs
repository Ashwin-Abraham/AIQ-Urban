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
public class RemoveZoneStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("RemoveExisting")]
    [TestCase("RemoveMultiple")]
    [TestCase("RemoveNonexistent")]
    [TestCase("EmptyIdList")]
    [TestCase("PartialMatch")]
    public void Run_RemovesZones(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange: add zones and capture their actual geometry IDs
        IUrbanModelStep addStep = new AddZoneStep();
        var addedIds = addStep.Run(UrbanModel.Instance, testData.InitialZones.Cast<ICreationData>().ToList());

        var initialCount = UrbanModel.Instance.Zones.Count;

        // Act
        IUrbanModelStep removeStep = new RemoveZoneStep(testData.GetRemoveIds(addedIds));
        var removedIds = removeStep.Run(UrbanModel.Instance, []);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeZones();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Zones, Has.Count.EqualTo(testData.ExpectedRemainingCount));
        Assert.That(removedIds, Has.Count.EqualTo(testData.ExpectedRemovedIdCount));

        if (testData.RemoveIndices.Count > 0 && testData.ExpectedRemainingCount < initialCount)
        {
            var remainingIds = UrbanModel.Instance.Zones
                .SelectMany(x => x.GetGeometryIds())
                .ToHashSet();
            Assert.That(removedIds.Any(id => remainingIds.Contains(id)), Is.False);
        }
    }

    private record RemoveZoneTestData(
        string CaseName,
        List<ZoneData> InitialZones,
        List<int> RemoveIndices,
        int ExpectedRemainingCount,
        int ExpectedRemovedIdCount)
    {
        public List<Guid> GetRemoveIds(List<Guid> addedIds)
        {
            if (RemoveIndices.Count > 0)
                return RemoveIndices.Select(i => addedIds[i]).ToList();
            if (InitialZones.Count > 0)
                return [Guid.NewGuid()];
            return [];
        }
    }

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

    private static readonly List<RemoveZoneTestData> _testData =
    [
        new RemoveZoneTestData(
            CaseName: "RemoveExisting",
            InitialZones:
            [
                new ZoneData(MakeBoundary(0, 0), "ZoneA"),
            ],
            RemoveIndices: [0],        // first (and only) geometry ID of zone 1
            ExpectedRemainingCount: 0,
            ExpectedRemovedIdCount: 1),

        new RemoveZoneTestData(
            CaseName: "RemoveMultiple",
            InitialZones:
            [
                new ZoneData(MakeBoundary(0, 0), "ZoneA"),
                new ZoneData(MakeBoundary(20, 0), "ZoneB"),
            ],
            RemoveIndices: [0, 1],     // first ID of each zone
            ExpectedRemainingCount: 0,
            ExpectedRemovedIdCount: 2),

        new RemoveZoneTestData(
            CaseName: "RemoveNonexistent",
            InitialZones:
            [
                new ZoneData(MakeBoundary(0, 0), "ZoneA"),
            ],
            RemoveIndices: [],         // triggers non-existent GUID
            ExpectedRemainingCount: 1,
            ExpectedRemovedIdCount: 0),

        new RemoveZoneTestData(
            CaseName: "EmptyIdList",
            InitialZones: [],
            RemoveIndices: [],
            ExpectedRemainingCount: 0,
            ExpectedRemovedIdCount: 0),

        new RemoveZoneTestData(
            CaseName: "PartialMatch",
            InitialZones:
            [
                new ZoneData(MakeBoundary(0, 0), "ZoneA"),
                new ZoneData(MakeBoundary(20, 0), "ZoneB"),
            ],
            RemoveIndices: [0],        // only match first zone
            ExpectedRemainingCount: 1,
            ExpectedRemovedIdCount: 1),
    ];
}
