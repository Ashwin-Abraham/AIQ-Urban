using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.Urban.Core.Tests.ModelActions;

[RhinoTestFixture]
public class ParcelBoundarySplittingTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("ClosedBoundarySplitsHoleIntoTwo")]
    [TestCase("OpenBoundarySplitsHoleIntoTwo")]
    [TestCase("BoundaryOutsideHoleNoSplit")]
    [TestCase("NoBoundariesNoSplit")]
    [TestCase("TwoBoundariesSplitIntoFour")]
    public void Run_SplitsParcelsCorrectly(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange — add transits to form enclosure
        IUrbanModelStep addTransitStep = new AddTransitStep();
        addTransitStep.Run(UrbanModel.Instance, testData.TransitInputs.Cast<ICreationData>().ToList());

        // Arrange — add parcel boundaries
        if (testData.BoundaryInputs.Count > 0)
        {
            IUrbanModelStep addBoundaryStep = new AddParcelBoundaryStep();
            addBoundaryStep.Run(UrbanModel.Instance, testData.BoundaryInputs.Cast<ICreationData>().ToList());
        }

        // Act — union transit surfaces, then recalculate parcels
        var unionerStep = new TransitSurfaceUnioner();
        unionerStep.Run(UrbanModel.Instance, []);
        var parcelsStep = new RecalculateParcelsStep();
        var result = parcelsStep.Run(UrbanModel.Instance, []);

        // Assert
        Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(testData.ExpectedParcelCount));
        Assert.That(result.AddedIds, Has.Count.EqualTo(testData.ExpectedAddedCount));

        for (int i = 0; i < testData.ExpectedParcelCount; i++)
        {
            Assert.That(UrbanModel.Instance.Parcels[i].Use.Name,
                Is.EqualTo(testData.ExpectedUses[i].Name));
        }
    }

    private record SplittingTestData(
        string CaseName,
        List<TransitData> TransitInputs,
        List<ParcelBoundaryData> BoundaryInputs,
        int ExpectedParcelCount,
        int ExpectedAddedCount,
        List<Use> ExpectedUses);

    private static Curve2D MakeCenterline(Point3d from, Point3d to)
        => new(new LineCurve(from, to), Guid.NewGuid());

    private static Curve2D MakeOpenBoundary(Point3d from, Point3d to)
        => new(new LineCurve(from, to), Guid.NewGuid());

    private static Curve2D MakeClosedBoundary(Point3d[] corners)
        => new(new PolylineCurve(corners.Append(corners[0]).ToArray()), Guid.NewGuid());

    // Rectangle enclosure: roads at y=0, y=30, x=0, x=30 (width 6)
    private static readonly List<TransitData> _rectEnclosure =
    [
        new TransitData(MakeCenterline(new(0, 0, 0), new(30, 0, 0)), 6, TransitType.Road),
        new TransitData(MakeCenterline(new(30, 0, 0), new(30, 30, 0)), 6, TransitType.Road),
        new TransitData(MakeCenterline(new(30, 30, 0), new(0, 30, 0)), 6, TransitType.Road),
        new TransitData(MakeCenterline(new(0, 30, 0), new(0, 0, 0)), 6, TransitType.Road),
    ];

    private static readonly List<SplittingTestData> _testData = BuildTestData();

    private static List<SplittingTestData> BuildTestData()
    {
        return
        [
            // No boundaries — same as existing SingleHole test, 1 parcel
            new(CaseName: "NoBoundariesNoSplit",
                TransitInputs: _rectEnclosure,
                BoundaryInputs: [],
                ExpectedParcelCount: 1,
                ExpectedAddedCount: 1,
                ExpectedUses: [DefaultUses.Create("Public")]),

            // Closed boundary inside the hole — splits into 2 parcels
            // Boundary: rectangle from (10,10) to (20,20) inside the 30x30 enclosure
            new(CaseName: "ClosedBoundarySplitsHoleIntoTwo",
                TransitInputs: _rectEnclosure,
                BoundaryInputs:
                [
                    new ParcelBoundaryData(MakeClosedBoundary([
                        new Point3d(10, 10, 0),
                        new Point3d(20, 10, 0),
                        new Point3d(20, 20, 0),
                        new Point3d(10, 20, 0),
                    ])),
                ],
                ExpectedParcelCount: 2,
                ExpectedAddedCount: 2,
                ExpectedUses:
                [
                    DefaultUses.Create("Public"),
                    DefaultUses.Create("Public"),
                ]),

            // Open boundary across the hole — splits into 2 parcels
            // Boundary: vertical line at x=15 from y=3 to y=27 (spanning the hole)
            new(CaseName: "OpenBoundarySplitsHoleIntoTwo",
                TransitInputs: _rectEnclosure,
                BoundaryInputs:
                [
                    new ParcelBoundaryData(MakeOpenBoundary(
                        new Point3d(15, 3, 0), new Point3d(15, 27, 0))),
                ],
                ExpectedParcelCount: 2,
                ExpectedAddedCount: 2,
                ExpectedUses:
                [
                    DefaultUses.Create("Public"),
                    DefaultUses.Create("Public"),
                ]),

            // Boundary entirely outside the hole — no splitting, still 1 parcel
            new(CaseName: "BoundaryOutsideHoleNoSplit",
                TransitInputs: _rectEnclosure,
                BoundaryInputs:
                [
                    new ParcelBoundaryData(MakeClosedBoundary([
                        new Point3d(50, 50, 0),
                        new Point3d(60, 50, 0),
                        new Point3d(60, 60, 0),
                        new Point3d(50, 60, 0),
                    ])),
                ],
                ExpectedParcelCount: 1,
                ExpectedAddedCount: 1,
                ExpectedUses: [DefaultUses.Create("Public")]),

            // Two open boundaries crossing — splits into 4 parcels
            // Horizontal at y=15, vertical at x=15
            new(CaseName: "TwoBoundariesSplitIntoFour",
                TransitInputs: _rectEnclosure,
                BoundaryInputs:
                [
                    new ParcelBoundaryData(MakeOpenBoundary(
                        new Point3d(3, 15, 0), new Point3d(27, 15, 0))),
                    new ParcelBoundaryData(MakeOpenBoundary(
                        new Point3d(15, 3, 0), new Point3d(15, 27, 0))),
                ],
                ExpectedParcelCount: 4,
                ExpectedAddedCount: 4,
                ExpectedUses:
                [
                    DefaultUses.Create("Public"),
                    DefaultUses.Create("Public"),
                    DefaultUses.Create("Public"),
                    DefaultUses.Create("Public"),
                ]),
        ];
    }
}
