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
public class AddTransitStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("SingleRoad")]
    [TestCase("MultipleTransitsDifferentTypes")]
    [TestCase("EmptyList")]
    [TestCase("Closed Polygon")]
    public void Run_AddsTransitsToModel(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        IUrbanModelStep step = new AddTransitStep();

        // Act
        var result = step.Run(UrbanModel.Instance, new CreationDataCollection<TransitData>(testData.Input));

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeTransits();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(testData.ExpectedCount));
        Assert.That(result, Has.Count.EqualTo(testData.ExpectedGeometryIdCount));

        if (testData.ExpectedCount > 0)
        {
            var allGeometryIds = UrbanModel.Instance.Transits
                .SelectMany(x => x.GetGeometryIds())
                .OrderBy(x => x)
                .ToList();
            var returnedIds = result.OrderBy(x => x).ToList();
            Assert.That(returnedIds, Is.EqualTo(allGeometryIds));
        }
    }

    private record AddTransitTestData(
        string CaseName,
        List<TransitData> Input,
        int ExpectedCount,
        int ExpectedGeometryIdCount);

    private static Curve2D MakeCenterline(params Point3d[] points)
    {
        return new Curve2D(new PolylineCurve(points), Guid.NewGuid());
    }

    private static readonly List<AddTransitTestData> _testData =
    [
        new AddTransitTestData(
            CaseName: "SingleRoad",
            Input: [new TransitData(MakeCenterline(new(0, 0, 0), new(50, 0, 0)), 6, TransitType.Road)],
            ExpectedCount: 1,
            ExpectedGeometryIdCount: 2),

        new AddTransitTestData(
            CaseName: "MultipleTransitsDifferentTypes",
            Input:
            [
                new TransitData(MakeCenterline(new(0, 0, 0), new(50, 0, 0)), 6, TransitType.Road),
                new TransitData(MakeCenterline(new(0, 10, 0), new(30, 10, 0)), 4, TransitType.CyclePath),
            ],
            ExpectedCount: 2,
            ExpectedGeometryIdCount: 4),

        new AddTransitTestData(
            CaseName: "EmptyList",
            Input: [],
            ExpectedCount: 0,
            ExpectedGeometryIdCount: 0),

        new AddTransitTestData(
            CaseName: "Closed Polygon",
            Input: [new TransitData(
                MakeCenterline(new(), new(50, 0, 0), new(50,50,0), new(0,50,0), new()),
                6,
                TransitType.Road)],
            ExpectedCount: 1,
            ExpectedGeometryIdCount: 2),
    ];
}
