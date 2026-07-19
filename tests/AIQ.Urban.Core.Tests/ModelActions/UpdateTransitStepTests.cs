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
public class UpdateTransitStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("UpdateExisting")]
    [TestCase("UpdateNonexistent")]
    public void Run_UpdatesTransit(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange: add initial transits and capture their actual geometry IDs
        IUrbanModelStep addStep = new AddTransitStep();
        var addedIds = addStep.Run(UrbanModel.Instance, testData.InitialTransits.Cast<ICreationData>().ToList());

        var oldCenterlineId = addedIds.AddedIds[0];
        var oldCount = UrbanModel.Instance.Transits.Count;

        // Act
        var updateStep = new UpdateTransitStep();
        var result = updateStep.Run(UrbanModel.Instance, [new TransitUpdateData(testData.UseExistingId ? oldCenterlineId : Guid.NewGuid(), testData.NewCenterline)]);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeTransits();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(testData.ExpectedTransitCount));

        if (testData.UseExistingId)
        {
            // The old centerline ID should no longer be present
            var allCurrentIds = UrbanModel.Instance.Transits
                .SelectMany(t => t.GetGeometryIds())
                .ToHashSet();
            Assert.That(allCurrentIds, Does.Not.Contain(oldCenterlineId));

            var allResultIds = result.AddedIds.Concat(result.DeletedIds).ToList();
            Assert.That(result.AddedIds, Has.Count.EqualTo(testData.ExpectedAddedCount));
            Assert.That(result.DeletedIds, Has.Count.EqualTo(testData.ExpectedDeletedCount));
            Assert.That(allResultIds, Has.All.Not.EqualTo(Guid.Empty));
            Assert.That(allResultIds, Is.Unique);

            // The new transit should have the new centerline geometry
            var updatedTransit = UrbanModel.Instance.Transits
                .FirstOrDefault(t => t.GetGeometryIds().Any(id => result.AddedIds.Contains(id)));
            Assert.That(updatedTransit, Is.Not.Null);
            Assert.That(updatedTransit!.Width, Is.EqualTo(testData.ExpectedWidth));
            Assert.That(updatedTransit.TransitType, Is.EqualTo(testData.ExpectedType));
        }
        else
        {
            // Non-existent ID: no changes
            Assert.That(result.AddedIds, Is.Empty);
            Assert.That(result.DeletedIds, Is.Empty);
            Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(oldCount));
        }
    }

    private record UpdateTransitTestData(
        string CaseName,
        List<TransitData> InitialTransits,
        bool UseExistingId,
        Curve NewCenterline,
        int ExpectedTransitCount,
        int ExpectedAddedCount,
        int ExpectedDeletedCount,
        double ExpectedWidth,
        TransitType ExpectedType);

    private static readonly List<UpdateTransitTestData> _testData = BuildTestData();

    private static List<UpdateTransitTestData> BuildTestData()
    {
        var originalCenterline = new LineCurve(new Point3d(0, 0, 0), new Point3d(50, 0, 0));
        var newCenterline = new LineCurve(new Point3d(0, 5, 0), new Point3d(50, 5, 0));

        var roadData = new TransitData(
            new Curve2D(originalCenterline, Guid.NewGuid()),
            6, TransitType.Road);

        return
        [
            new UpdateTransitTestData(
                CaseName: "UpdateExisting",
                InitialTransits: [roadData],
                UseExistingId: true,
                NewCenterline: newCenterline,
                ExpectedTransitCount: 1,
                ExpectedAddedCount: 2,  // new centerline + new outline
                ExpectedDeletedCount: 2,  // old centerline + old outline
                ExpectedWidth: 6,
                ExpectedType: TransitType.Road),

            new UpdateTransitTestData(
                CaseName: "UpdateNonexistent",
                InitialTransits: [roadData],
                UseExistingId: false,
                NewCenterline: newCenterline,
                ExpectedTransitCount: 1,
                ExpectedAddedCount: 0,
                ExpectedDeletedCount: 0,
                ExpectedWidth: 0,
                ExpectedType: TransitType.Road),
        ];
    }
}
