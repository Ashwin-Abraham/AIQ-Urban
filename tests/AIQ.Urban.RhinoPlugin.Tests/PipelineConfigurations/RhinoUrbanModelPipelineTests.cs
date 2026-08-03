using System.Collections.Generic;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;
using NUnit.Framework;
using Rhino;
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
    public void Run_WithInputPreparationStep_ExecutesPreparationExactlyOnce()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var preparationStep = new CountingInputPreparationStep();
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, preparationStep);

        // Act
        pipeline.Run();

        // Assert
        Assert.That(preparationStep.ExecutionCount, Is.EqualTo(1));
    }

    private sealed class CountingInputPreparationStep : IInputPreparationStep
    {
        public int ExecutionCount { get; private set; }

        public List<ICreationData> Run(InputPreparationContext context)
        {
            ExecutionCount++;
            return [];
        }
    }
}
