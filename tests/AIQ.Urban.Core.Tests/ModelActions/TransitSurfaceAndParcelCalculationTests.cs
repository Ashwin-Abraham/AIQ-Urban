using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.Urban.Core.Tests.ModelActions;

[RhinoTestFixture]
public class TransitSurfaceAndParcelCalculationTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("NoTransits")]
    [TestCase("NoHoles")]
    [TestCase("SingleHole")]
    [TestCase("TwoSeparateHoles")]
    public void Run_RegeneratesParcelsFromHoles(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        IUrbanModelStep addStep = new AddTransitStep();
        addStep.Run(UrbanModel.Instance, testData.TransitInputs.Cast<ICreationData>().ToList());

        // Act
        var unionerStep = new TransitSurfaceUnioner();
        unionerStep.Run(UrbanModel.Instance, []);
        var parcelsStep = new RecalculateParcelsStep();
        var result = parcelsStep.Run(UrbanModel.Instance, []);

        // Visualise
#if DEBUG
        var vizDoc = new UrbanSceneBuilder()
            .AddUrbanElements(UrbanModel.Instance.Parcels.Cast<IUrbanElement>().ToList())
            .AddTransitSurfaces(UrbanModel.Instance.TransitSurfaces)
            .Build();
        Visualizer.View(vizDoc);
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(testData.ExpectedParcelCount));
        Assert.That(result.AddedIds, Has.Count.EqualTo(testData.AddedCount));
        Assert.That(result.DeletedIds, Has.Count.EqualTo(testData.DeletedCount));

        for (int i = 0; i < testData.ExpectedParcelCount; i++)
        {
            Assert.That(UrbanModel.Instance.Parcels[i].Use.Name, Is.EqualTo(testData.ExpectedUses[i].Name));
            Assert.That(UrbanModel.Instance.Parcels[i].GetGeometryIds(), Has.Count.EqualTo(1));
        }
    }

    private record RecalculateParcelsTestData(
        string CaseName,
        List<TransitData> TransitInputs,
        int ExpectedParcelCount,
        int AddedCount,
        int DeletedCount,
        List<Use> ExpectedUses);

    private static Curve2D MakeCenterline(Point3d from, Point3d to)
        => new(new LineCurve(from, to), Guid.NewGuid());

    private static readonly List<RecalculateParcelsTestData> _testData = BuildTestData();

    private static List<RecalculateParcelsTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "NoTransits",
                TransitInputs: [],
                ExpectedParcelCount: 0,
                AddedCount: 0,
                DeletedCount: 0,
                ExpectedUses: []),

            new(CaseName: "NoHoles",
                TransitInputs:
                [
                    new TransitData(MakeCenterline(new(0, 0, 0), new(30, 0, 0)), 6, TransitType.Road),
                ],
                ExpectedParcelCount: 0,
                AddedCount: 0,
                DeletedCount: 0,
                ExpectedUses: []),

            new(CaseName: "SingleHole",
                TransitInputs:
                [
                    new TransitData(MakeCenterline(new(0, 0, 0), new(30, 0, 0)), 6, TransitType.Road),
                    new TransitData(MakeCenterline(new(30, 0, 0), new(30, 30, 0)), 6, TransitType.Road),
                    new TransitData(MakeCenterline(new(30, 30, 0), new(0, 30, 0)), 6, TransitType.Road),
                    new TransitData(MakeCenterline(new(0, 30, 0), new(0, 0, 0)), 6, TransitType.Road),
                ],
                ExpectedParcelCount: 1,
                AddedCount: 1,
                DeletedCount: 0,
                ExpectedUses: [DefaultUses.Create("Public")]),

            new(CaseName: "TwoSeparateHoles",
                TransitInputs:
                [
                    // Ring 1
                    new TransitData(MakeCenterline(new(0, 0, 0), new(30, 0, 0)), 6, TransitType.Road),
                    new TransitData(MakeCenterline(new(30, 0, 0), new(30, 30, 0)), 6, TransitType.Road),
                    new TransitData(MakeCenterline(new(30, 30, 0), new(0, 30, 0)), 6, TransitType.Road),
                    new TransitData(MakeCenterline(new(0, 30, 0), new(0, 0, 0)), 6, TransitType.Road),
                    // Ring 2 (offset by 50 in Y)
                    new TransitData(MakeCenterline(new(0, 50, 0), new(30, 50, 0)), 6, TransitType.Road),
                    new TransitData(MakeCenterline(new(30, 50, 0), new(30, 80, 0)), 6, TransitType.Road),
                    new TransitData(MakeCenterline(new(30, 80, 0), new(0, 80, 0)), 6, TransitType.Road),
                    new TransitData(MakeCenterline(new(0, 80, 0), new(0, 50, 0)), 6, TransitType.Road),
                ],
                ExpectedParcelCount: 2,
                AddedCount: 2,
                DeletedCount: 0,
                ExpectedUses: [DefaultUses.Create("Public"), DefaultUses.Create("Public")]),
        ];
    }
}
