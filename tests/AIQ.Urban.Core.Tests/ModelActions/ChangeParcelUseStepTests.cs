using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.EditData;
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
public class ChangeParcelUseStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("SingleParcel")]
    [TestCase("MultipleParcels")]
    [TestCase("EmptyIdList")]
    [TestCase("PartialMatch")]
    public void Run_ChangesParcelUse(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        foreach (var parcel in testData.InitialParcels)
            UrbanModel.Instance.Parcels.Add(parcel);

        var step = new ChangeParcelUseStep();

        // Act
        var result = step.Run(UrbanModel.Instance, [new ParcelUseChangeData(testData.ParcelIdsToChange, testData.NewUse)]);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeParcels();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(testData.InitialParcels.Count));

        for (int i = 0; i < testData.ExpectedUses.Count; i++)
            Assert.That(UrbanModel.Instance.Parcels[i].Use, Is.SameAs(testData.ExpectedUses[i]));

        Assert.That(result.AddedIds.Count, Is.EqualTo(testData.AddedCount));
        Assert.That(result.DeletedIds.Count, Is.EqualTo(testData.DeletedCount));
    }

    private record ChangeParcelUseTestData(
        string CaseName,
        List<Parcel> InitialParcels,
        List<Guid> ParcelIdsToChange,
        Use NewUse,
        List<Use> ExpectedUses,
        int AddedCount,
        int DeletedCount);

    private static PolyCurve2D MakeOutline(double size)
    {
        return new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(size, 0, 0),
            new Point3d(size, size, 0),
            new Point3d(0, size, 0),
            new Point3d(0, 0, 0),
        ]))
            .ToPolyCurve2d();
    }

    private static readonly List<ChangeParcelUseTestData> _testData = BuildTestData();

    private static List<ChangeParcelUseTestData> BuildTestData()
    {
        var sp = MakeOutline(10);
        var mp1 = MakeOutline(10);
        var mp2 = MakeOutline(5);
        var pm = MakeOutline(10);

        var publicUse = DefaultUses.Create("Public");
        var residentialUse = DefaultUses.Create("Residential");
        var commercialUse = DefaultUses.Create("Commercial");
        var retailUse = DefaultUses.Create("Retail");

        return
        [
            new ChangeParcelUseTestData(
                "SingleParcel",
                InitialParcels: [new Parcel(sp, publicUse)],
                ParcelIdsToChange: [sp.Id],
                NewUse: residentialUse,
                ExpectedUses: [residentialUse],
                AddedCount: 1,
                DeletedCount: 1),

            new ChangeParcelUseTestData(
                "MultipleParcels",
                InitialParcels: [new Parcel(mp1, publicUse), new Parcel(mp2, publicUse)],
                ParcelIdsToChange: [mp1.Id, mp2.Id],
                NewUse: commercialUse,
                ExpectedUses: [commercialUse, commercialUse],
                AddedCount: 2,
                DeletedCount: 2),

            new ChangeParcelUseTestData(
                "EmptyIdList",
                InitialParcels: [new Parcel(MakeOutline(10), publicUse)],
                ParcelIdsToChange: [],
                NewUse: residentialUse,
                ExpectedUses: [publicUse],
                AddedCount: 0,
                DeletedCount: 0),

            new ChangeParcelUseTestData(
                "PartialMatch",
                InitialParcels: [new Parcel(pm, publicUse)],
                ParcelIdsToChange: [pm.Id, Guid.NewGuid(), Guid.NewGuid()],
                NewUse: retailUse,
                ExpectedUses: [retailUse],
                AddedCount: 1,
                DeletedCount: 1),
        ];
    }
}
