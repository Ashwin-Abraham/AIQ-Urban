using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Tests.ModelActions.Tools;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.Urban.Core.Tests.ModelActions;

[RhinoTestFixture]
public class RecalculateParcelsStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("NoTransitSurfaces")]
    [TestCase("SingleHoleFirstRun")]
    [TestCase("MultipleHoles")]
    [TestCase("MultipleSurfaces")]
    [TestCase("UsePreservation")]
    [TestCase("NoHoles")]
    public void Run_RegeneratesParcelsFromHoles(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        foreach (var (profile, type) in testData.InitialSurfaces)
            UrbanModel.Instance.TransitSurfaces.Add((profile, type));

        foreach (var parcel in testData.InitialParcels)
            UrbanModel.Instance.Parcels.Add(parcel);

        // Act
        IUrbanModelStep step = new RecalculateParcelsStep();
        var result = step.Run(UrbanModel.Instance);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeParcels();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(testData.ExpectedParcelCount));
        Assert.That(result, Has.Count.EqualTo(testData.ExpectedReturnedIdCount));

        for (int i = 0; i < testData.ExpectedParcelCount; i++)
        {
            Assert.That(UrbanModel.Instance.Parcels[i].Use, Is.EqualTo(testData.ExpectedUses[i]));
            Assert.That(UrbanModel.Instance.Parcels[i].GetGeometryIds(), Has.Count.EqualTo(1));
        }
    }

    private record RecalculateParcelsTestData(
        string CaseName,
        List<(Profile2D Profile, TransitType Type)> InitialSurfaces,
        List<Parcel> InitialParcels,
        int ExpectedParcelCount,
        int ExpectedReturnedIdCount,
        List<Use> ExpectedUses);

    private static Polygon2D MakePolygon(double x1, double y1, double x2, double y2)
        => new(new PolylineCurve([
            new Point3d(x1, y1, 0),
            new Point3d(x2, y1, 0),
            new Point3d(x2, y2, 0),
            new Point3d(x1, y2, 0),
            new Point3d(x1, y1, 0),
        ]));

    private static Profile2D MakeDonutWithHoles(int holeCount)
    {
        var outer = MakePolygon(-10, -10, 30, 30);
        var holes = new List<Polygon2D>();
        for (int i = 0; i < holeCount; i++)
            holes.Add(MakePolygon(2 + i * 12, 2, 8 + i * 12, 8));

        return Profile2D.Create(outer, holes)!;
    }

    private static readonly List<RecalculateParcelsTestData> _testData = BuildTestData();

    private static List<RecalculateParcelsTestData> BuildTestData()
    {
        var surfaceWithOneHole = MakeDonutWithHoles(1);
        var surfaceWithTwoHoles = MakeDonutWithHoles(2);

        return
        [
            new(CaseName: "NoTransitSurfaces",
                InitialSurfaces: [],
                InitialParcels: [],
                ExpectedParcelCount: 0,
                ExpectedReturnedIdCount: 0,
                ExpectedUses: []),

            new(CaseName: "NoHoles",
                InitialSurfaces: [(MakeDonutWithHoles(0), TransitType.Road)],
                InitialParcels: [],
                ExpectedParcelCount: 0,
                ExpectedReturnedIdCount: 0,
                ExpectedUses: []),

            new(CaseName: "SingleHoleFirstRun",
                InitialSurfaces: [(surfaceWithOneHole, TransitType.Road)],
                InitialParcels: [],
                ExpectedParcelCount: 1,
                ExpectedReturnedIdCount: 1,
                ExpectedUses: [DefaultUses.Public]),

            new(CaseName: "MultipleHoles",
                InitialSurfaces: [(surfaceWithTwoHoles, TransitType.Road)],
                InitialParcels: [],
                ExpectedParcelCount: 2,
                ExpectedReturnedIdCount: 2,
                ExpectedUses: [DefaultUses.Public, DefaultUses.Public]),

            new(CaseName: "MultipleSurfaces",
                InitialSurfaces:
                [
                    (surfaceWithOneHole, TransitType.Road),
                    (surfaceWithOneHole, TransitType.Railway),
                ],
                InitialParcels: [],
                ExpectedParcelCount: 2,
                ExpectedReturnedIdCount: 2,
                ExpectedUses: [DefaultUses.Public, DefaultUses.Public]),

            new(CaseName: "UsePreservation",
                InitialSurfaces: [(surfaceWithOneHole, TransitType.Road)],
                InitialParcels: [new Parcel(MakePolygon(2, 2, 8, 8), DefaultUses.Residential)],
                ExpectedParcelCount: 1,
                ExpectedReturnedIdCount: 2,
                ExpectedUses: [DefaultUses.Residential]),
        ];
    }
}
