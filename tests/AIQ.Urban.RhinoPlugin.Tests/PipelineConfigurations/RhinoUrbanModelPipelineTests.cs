using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.RhinoInterface.Pipeline.EditData;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests;

[RhinoTestFixture]
public class RhinoUrbanModelPipelineTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public void Run_MultipleGatherSteps_AggregatesDataInRegistrationOrder()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var gatherOrder = new List<int>();
        var modelStep = new CapturingModelStep();

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(new StubGatherStep(
                [new SequenceData(1), new SequenceData(2)],
                () => gatherOrder.Add(1)))
            .WithDataGatherStep(new StubGatherStep(
                [new SequenceData(3)],
                () => gatherOrder.Add(2)))
            .WithUrbanElementStep(modelStep);

        pipeline.Run();

        Assert.That(gatherOrder, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(
            modelStep.ReceivedData.OfType<SequenceData>().Select(data => data.Value),
            Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Run_PreservationFromSeparateGatherStep_OverridesStagedDeletion()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var deletedId = doc.Objects.AddLine(new Line(Point3d.Origin, new Point3d(1, 0, 0)));
        var preservedId = doc.Objects.AddLine(new Line(Point3d.Origin, new Point3d(0, 1, 0)));

        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(new StubGatherStep([
                new RhinoObjectDeletionData(deletedId),
                new RhinoObjectDeletionData(preservedId)
            ]))
            .WithDataGatherStep(new StubGatherStep([new RhinoObjectPreservationData(preservedId)]))
            .WithRhinoOutputter(new ParcelBoundaryOutputter())
            .Run();

        Assert.That(doc.Objects.FindId(deletedId), Is.Null);
        Assert.That(doc.Objects.FindId(preservedId), Is.Not.Null);
    }

    private record SequenceData(int Value) : IModelEditData;

    private class StubGatherStep(List<IModelEditData> data, Action onRun = null) : IRhinoDataGatherStep
    {
        public List<IModelEditData> Run(RhinoDoc doc)
        {
            onRun?.Invoke();
            return data;
        }
    }

    private class CapturingModelStep : IUrbanModelStep
    {
        public List<IModelEditData> ReceivedData { get; private set; } = [];

        public StepResult Run(UrbanModel model, List<IModelEditData> creationData)
        {
            ReceivedData = creationData;
            return new([], []);
        }
    }
}
