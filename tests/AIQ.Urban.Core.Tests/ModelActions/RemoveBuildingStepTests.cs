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
public class RemoveBuildingStepTests
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
    public void Run_RemovesBuildings(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange: add buildings and capture their actual geometry IDs
        IUrbanModelStep addStep = new AddBuildingStep();
        var addedIds = addStep.Run(UrbanModel.Instance, testData.InitialBuildings.Cast<ICreationData>().ToList());

        var initialCount = UrbanModel.Instance.Buildings.Count;

        // Act
        IUrbanModelStep removeStep = new RemoveBuildingStep(testData.GetRemoveIds(addedIds));
        var removedIds = removeStep.Run(UrbanModel.Instance, []);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeBuildings();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(testData.ExpectedRemainingCount));
        Assert.That(removedIds, Has.Count.EqualTo(testData.ExpectedRemovedIdCount));

        if (testData.RemoveIndices.Count > 0 && testData.ExpectedRemainingCount < initialCount)
        {
            var remainingIds = UrbanModel.Instance.Buildings
                .SelectMany(x => x.GetGeometryIds())
                .ToHashSet();
            Assert.That(removedIds.Any(id => remainingIds.Contains(id)), Is.False);
        }
    }

    private record RemoveBuildingTestData(
        string CaseName,
        List<BuildingFootprintData> InitialBuildings,
        List<int> RemoveIndices,
        int ExpectedRemainingCount,
        int ExpectedRemovedIdCount)
    {
        /// <summary>Resolves removal indices to actual GUIDs from the add step result.
        /// When indices is empty and there are initial buildings, passes a non-existent GUID
        /// to test no-op behaviour (RemoveNonexistent).</summary>
        public List<Guid> GetRemoveIds(List<Guid> addedIds)
        {
            if (RemoveIndices.Count > 0)
                return RemoveIndices.Select(i => addedIds[i]).ToList();
            if (InitialBuildings.Count > 0)
                return [Guid.NewGuid()]; // non-existent
            return [];
        }
    }

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

    private static readonly List<RemoveBuildingTestData> _testData =
    [
        // 5 floors → 6 geometry IDs per building (5 floor plates + 1 envelope)
        new RemoveBuildingTestData(
            CaseName: "RemoveExisting",
            InitialBuildings:
            [
                new BuildingFootprintData(SimpleFootprint, 3.5, 5, DefaultUses.Residential),
            ],
            RemoveIndices: [0],        // match first geometry ID of building 1
            ExpectedRemainingCount: 0,
            ExpectedRemovedIdCount: 6),

        new RemoveBuildingTestData(
            CaseName: "RemoveMultiple",
            InitialBuildings:
            [
                new BuildingFootprintData(SimpleFootprint, 3.5, 5, DefaultUses.Residential),
                new BuildingFootprintData(SmallFootprint, 4.0, 3, DefaultUses.Commercial),
            ],
            RemoveIndices: [0, 6],     // first ID of building 1 (idx 0) and building 2 (idx 6)
            ExpectedRemainingCount: 0,
            ExpectedRemovedIdCount: 10),

        new RemoveBuildingTestData(
            CaseName: "RemoveNonexistent",
            InitialBuildings:
            [
                new BuildingFootprintData(SimpleFootprint, 3.5, 5, DefaultUses.Residential),
            ],
            RemoveIndices: [],         // triggers non-existent GUID
            ExpectedRemainingCount: 1,
            ExpectedRemovedIdCount: 0),

        new RemoveBuildingTestData(
            CaseName: "EmptyIdList",
            InitialBuildings: [],
            RemoveIndices: [],
            ExpectedRemainingCount: 0,
            ExpectedRemovedIdCount: 0),

        new RemoveBuildingTestData(
            CaseName: "PartialMatch",
            InitialBuildings:
            [
                new BuildingFootprintData(SimpleFootprint, 3.5, 5, DefaultUses.Residential),
                new BuildingFootprintData(SmallFootprint, 4.0, 3, DefaultUses.Commercial),
            ],
            RemoveIndices: [0],        // only match first building
            ExpectedRemainingCount: 1,
            ExpectedRemovedIdCount: 6),
    ];
}
