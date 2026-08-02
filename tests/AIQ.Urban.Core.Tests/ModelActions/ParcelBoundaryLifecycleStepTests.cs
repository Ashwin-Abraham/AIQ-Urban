using System;
using System.Linq;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.ModelActions;

[RhinoTestFixture]
public class ParcelBoundaryLifecycleStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public void RemoveParcelBoundaryStep_ExistingBoundary_RemovesBoundaryAndReportsDeletedId()
    {
        // Arrange
        var boundaryId = Guid.NewGuid();
        new AddParcelBoundaryStep().Run(UrbanModel.Instance, [
            new ParcelBoundaryData(new Curve2D(
                new LineCurve(new Point3d(0, 5, 0), new Point3d(10, 5, 0)),
                boundaryId)),
        ]);

        // Act
        var result = new RemoveParcelBoundaryStep().Run(UrbanModel.Instance, [
            new ParcelBoundaryDeletionData([boundaryId]),
        ]);

        // Preview
        TestContext.WriteLine($"Remaining boundaries: {UrbanModel.Instance.ParcelBoundaries.Count}");

        // Assert
        Assert.That(UrbanModel.Instance.ParcelBoundaries, Is.Empty);
        Assert.That(result.DeletedIds, Is.EqualTo(new[] { boundaryId }));
    }

    [Test]
    public void UpdateParcelBoundaryStep_ExistingBoundary_ReplacesGeometryAndReportsChangedIds()
    {
        // Arrange
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        new AddParcelBoundaryStep().Run(UrbanModel.Instance, [
            new ParcelBoundaryData(new Curve2D(
                new LineCurve(new Point3d(0, 5, 0), new Point3d(10, 5, 0)),
                oldId)),
        ]);
        var replacement = new Curve2D(
            new LineCurve(new Point3d(0, 7, 0), new Point3d(10, 7, 0)),
            newId);

        // Act
        var result = new UpdateParcelBoundaryStep().Run(UrbanModel.Instance, [
            new ParcelBoundaryUpdateData(oldId, replacement),
        ]);

        // Preview
        TestContext.WriteLine($"Updated boundary: {UrbanModel.Instance.ParcelBoundaries.Single().Id}");

        // Assert
        Assert.That(UrbanModel.Instance.ParcelBoundaries, Has.Count.EqualTo(1));
        Assert.That(UrbanModel.Instance.ParcelBoundaries[0].Id, Is.EqualTo(newId));
        Assert.That(result.AddedIds, Is.EqualTo(new[] { newId }));
        Assert.That(result.DeletedIds, Is.EqualTo(new[] { oldId }));
    }
}
