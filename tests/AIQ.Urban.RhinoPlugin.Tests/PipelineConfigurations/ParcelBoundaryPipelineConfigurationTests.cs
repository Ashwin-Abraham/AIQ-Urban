using System.Drawing;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.EventHandler;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using AIQ.Urban.RhinoInterface.Utils;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests;

[RhinoTestFixture]
public class ParcelBoundaryPipelineConfigurationTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public void WithParcelBoundaryUpdateSteps_ReplacementCurve_UpdatesManagedBoundary()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var attributes = RhinoOutputHelper.MakeLayerDrivenAttributes(
            doc,
            LayerConstants.ParcelBoundary,
            Color.DarkViolet);
        var oldId = doc.Objects.AddLine(
            new Line(new Point3d(0, 5, 0), new Point3d(10, 5, 0)),
            attributes);
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(new ParcelBoundaryAdditionGatherStep([
                new ParcelBoundaryData(new Curve2D(
                    new LineCurve(new Point3d(0, 5, 0), new Point3d(10, 5, 0)),
                    oldId)),
            ]))
            .WithParcelBoundaryAdditionSteps()
            .Run();
        var replacementCurve = new LineCurve(new Point3d(0, 7, 0), new Point3d(10, 7, 0));
        var replacementId = doc.Objects.AddCurve(replacementCurve);

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(new ParcelBoundaryUpdateGatherStep(oldId, replacementId, replacementCurve))
            .WithParcelBoundaryUpdateSteps()
            .WithFullModelOutput()
            .Run();

        // Preview
        var updated = UrbanModel.Instance.ParcelBoundaries.Single();
        TestContext.WriteLine($"Updated boundary ID: {updated.Id}");

        // Assert
        Assert.That(updated.Id, Is.Not.EqualTo(oldId));
        Assert.That(updated.Id, Is.Not.EqualTo(replacementId));
        Assert.That(updated.Curve.Geometry.PointAtStart.Y, Is.EqualTo(7));
        Assert.That(doc.Objects.FindId(oldId), Is.Null);
        Assert.That(doc.Objects.FindId(replacementId), Is.Null);
        Assert.That(doc.Objects.FindId(updated.Id), Is.Not.Null);
    }

    [Test]
    public void WithParcelBoundaryDeletionSteps_ManagedBoundary_RemovesBoundaryAndRhinoObject()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var boundaryId = doc.Objects.AddLine(
            new Line(new Point3d(0, 5, 0), new Point3d(10, 5, 0)));
        new AddParcelBoundaryStep().Run(UrbanModel.Instance, [
            new ParcelBoundaryData(new Curve2D(
                new LineCurve(new Point3d(0, 5, 0), new Point3d(10, 5, 0)),
                boundaryId)),
        ]);

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(new ParcelBoundaryDeletionGatherStep([boundaryId]))
            .WithParcelBoundaryDeletionSteps()
            .WithFullModelOutput()
            .Run();

        // Preview
        TestContext.WriteLine($"Remaining boundaries: {UrbanModel.Instance.ParcelBoundaries.Count}");

        // Assert
        Assert.That(UrbanModel.Instance.ParcelBoundaries, Is.Empty);
        Assert.That(doc.Objects.FindId(boundaryId), Is.Null);
    }

    [Test]
    public void HandleParcelBoundaryReplace_ValidCurve_UpdatesManagedBoundary()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var oldId = doc.Objects.AddLine(
            new Line(new Point3d(0, 5, 0), new Point3d(10, 5, 0)));
        var oldBoundary = new ParcelBoundary(new Curve2D(
            new LineCurve(new Point3d(0, 5, 0), new Point3d(10, 5, 0)),
            oldId));
        UrbanModel.Instance.AddParcelBoundary(oldBoundary);
        var replacementId = doc.Objects.AddLine(
            new Line(new Point3d(0, 7, 0), new Point3d(10, 7, 0)));
        using var handler = new RhinoUrbanEventHandler(doc);

        // Act
        handler.HandleParcelBoundaryReplace(oldBoundary, replacementId);

        // Preview
        var updated = UrbanModel.Instance.ParcelBoundaries.Single();
        TestContext.WriteLine($"Updated boundary Y: {updated.Curve.Geometry.PointAtStart.Y}");

        // Assert
        Assert.That(updated.Curve.Geometry.PointAtStart.Y, Is.EqualTo(7));
        Assert.That(doc.Objects.FindId(updated.Id), Is.Not.Null);
    }

    [Test]
    public void WithDocModelLoad_ManagedParcelBoundary_RestoresBoundaryBeforeParcelRecalculation()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var attributes = RhinoOutputHelper.MakeLayerDrivenAttributes(
            doc,
            LayerConstants.ParcelBoundary,
            Color.DarkViolet);
        var sourceId = doc.Objects.AddLine(
            new Line(new Point3d(0, 5, 0), new Point3d(10, 5, 0)),
            attributes);

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDocModelLoad(doc)
            .Run();

        // Preview
        TestContext.WriteLine($"Restored boundaries: {UrbanModel.Instance.ParcelBoundaries.Count}");

        // Assert
        Assert.That(UrbanModel.Instance.ParcelBoundaries, Has.Count.EqualTo(1));
        Assert.That(doc.Objects.FindId(sourceId), Is.Null);
        Assert.That(doc.Objects.FindId(UrbanModel.Instance.ParcelBoundaries[0].Id), Is.Not.Null);
    }
}
