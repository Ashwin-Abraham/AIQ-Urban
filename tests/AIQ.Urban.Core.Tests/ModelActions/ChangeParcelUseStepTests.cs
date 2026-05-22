using AIQ.Urban.Core.ModelActions;
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

        IUrbanModelStep step = new ChangeParcelUseStep(
            testData.ParcelIdsToChange,
            testData.NewUse);

        // Act
        var result = step.Run(UrbanModel.Instance);

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeParcels();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(testData.InitialParcels.Count));

        for (int i = 0; i < testData.ExpectedUses.Count; i++)
            Assert.That(UrbanModel.Instance.Parcels[i].Use, Is.EqualTo(testData.ExpectedUses[i]));

        Assert.That(result, Has.Count.EqualTo(testData.ExpectedResultCount));
    }

    private record ChangeParcelUseTestData(
        string CaseName,
        List<Parcel> InitialParcels,
        List<Guid> ParcelIdsToChange,
        Use NewUse,
        List<Use> ExpectedUses,
        int ExpectedResultCount);

    private static Polygon2D MakeOutline(double size)
    {
        return new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(size, 0, 0),
            new Point3d(size, size, 0),
            new Point3d(0, size, 0),
            new Point3d(0, 0, 0),
        ]));
    }

    private static readonly List<ChangeParcelUseTestData> _testData = BuildTestData();

    private static List<ChangeParcelUseTestData> BuildTestData()
    {
        var sp = MakeOutline(10);
        var mp1 = MakeOutline(10);
        var mp2 = MakeOutline(5);
        var pm = MakeOutline(10);

        return
        [
            new ChangeParcelUseTestData(
                "SingleParcel",
                InitialParcels: [new Parcel(sp, DefaultUses.Public)],
                ParcelIdsToChange: [sp.Id],
                NewUse: DefaultUses.Residential,
                ExpectedUses: [DefaultUses.Residential],
                ExpectedResultCount: 2),

            new ChangeParcelUseTestData(
                "MultipleParcels",
                InitialParcels: [new Parcel(mp1, DefaultUses.Public), new Parcel(mp2, DefaultUses.Public)],
                ParcelIdsToChange: [mp1.Id, mp2.Id],
                NewUse: DefaultUses.Commercial,
                ExpectedUses: [DefaultUses.Commercial, DefaultUses.Commercial],
                ExpectedResultCount: 4),

            new ChangeParcelUseTestData(
                "EmptyIdList",
                InitialParcels: [new Parcel(MakeOutline(10), DefaultUses.Public)],
                ParcelIdsToChange: [],
                NewUse: DefaultUses.Residential,
                ExpectedUses: [DefaultUses.Public],
                ExpectedResultCount: 0),

            new ChangeParcelUseTestData(
                "PartialMatch",
                InitialParcels: [new Parcel(pm, DefaultUses.Public)],
                ParcelIdsToChange: [pm.Id, Guid.NewGuid(), Guid.NewGuid()],
                NewUse: DefaultUses.Retail,
                ExpectedUses: [DefaultUses.Retail],
                ExpectedResultCount: 2),
        ];
    }
}
