using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
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
public class RemoveTransitStepTests
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
    public void Run_RemovesTransits(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        IUrbanModelStep addStep = new AddTransitStep();
        addStep.Run(UrbanModel.Instance, testData.InitialTransits.Cast<ICreationData>().ToList());

        var initialCount = UrbanModel.Instance.Transits.Count;

        // Act
        IUrbanModelStep removeStep = new RemoveTransitStep();
        var removeResult = removeStep.Run(UrbanModel.Instance, [new TransitDeletionData(testData.CenterlineIds)]);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeTransits();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(testData.ExpectedRemainingCount));
        Assert.That(removeResult.DeletedIds, Has.Count.EqualTo(testData.ExpectedRemovedIdCount));

        if (testData.CenterlineIds.Count > 0 && testData.ExpectedRemainingCount < initialCount)
        {
            var remainingIds = UrbanModel.Instance.Transits
                .SelectMany(t => t.GetGeometryIds())
                .ToHashSet();
            Assert.That(removeResult.DeletedIds.Any(id => remainingIds.Contains(id)), Is.False);
        }
    }

    private record RemoveTransitTestData(
        string CaseName,
        List<TransitData> InitialTransits,
        List<Guid> CenterlineIds,
        int ExpectedRemainingCount,
        int ExpectedRemovedIdCount);

    private static Curve2D MakeCenterline(Point3d from, Point3d to, out Guid id)
    {
        id = Guid.NewGuid();
        return new Curve2D(new LineCurve(from, to), id);
    }

    private static readonly List<RemoveTransitTestData> _testData = BuildTestData();

    private static List<RemoveTransitTestData> BuildTestData()
    {
        MakeCenterline(new(0, 0, 0), new(50, 0, 0), out var road1Id);
        MakeCenterline(new(0, 10, 0), new(50, 10, 0), out var road2Id);

        var road1Data = new TransitData(
            new Curve2D(new LineCurve(new Point3d(0, 0, 0), new Point3d(50, 0, 0)), road1Id),
            6, TransitType.Road);
        var road2Data = new TransitData(
            new Curve2D(new LineCurve(new Point3d(0, 10, 0), new Point3d(50, 10, 0)), road2Id),
            6, TransitType.Road);

        return
        [
            new RemoveTransitTestData(
                CaseName: "RemoveExisting",
                InitialTransits: [road1Data],
                CenterlineIds: [road1Id],
                ExpectedRemainingCount: 0,
                ExpectedRemovedIdCount: 2),

            new RemoveTransitTestData(
                CaseName: "RemoveMultiple",
                InitialTransits: [road1Data, road2Data],
                CenterlineIds: [road1Id, road2Id],
                ExpectedRemainingCount: 0,
                ExpectedRemovedIdCount: 4),

            new RemoveTransitTestData(
                CaseName: "RemoveNonexistent",
                InitialTransits: [road1Data],
                CenterlineIds: [Guid.NewGuid()],
                ExpectedRemainingCount: 1,
                ExpectedRemovedIdCount: 0),

            new RemoveTransitTestData(
                CaseName: "EmptyIdList",
                InitialTransits: [road1Data],
                CenterlineIds: [],
                ExpectedRemainingCount: 1,
                ExpectedRemovedIdCount: 0),

            new RemoveTransitTestData(
                CaseName: "PartialMatch",
                InitialTransits: [road1Data, road2Data],
                CenterlineIds: [road1Id],
                ExpectedRemainingCount: 1,
                ExpectedRemovedIdCount: 2),
        ];
    }
}
