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
using System.Drawing;
using System.Linq;

namespace AIQ.Urban.Core.Tests.ModelActions;

[RhinoTestFixture]
public class ModifyModelUsesStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("RemoveUndefined")]
    [TestCase("EditUndefinedName")]
    [TestCase("EditUndefinedColor")]
    public void Run_UndefinedUse_IsProtected(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        var undefinedUse = UrbanModel.Instance.GetUse(DefaultUses.UndefinedName);
        var parcel = new Parcel(MakeOutline(10), undefinedUse);
        UrbanModel.Instance.Parcels.Add(parcel);

        var step = new ModifyModelUsesStep();

        // Act
        var result = step.Run(UrbanModel.Instance, [new UseModificationData(testData.EditType, testData.NewName, testData.NewColor, undefinedUse.Id)]);

        // Assert
        Assert.That(result.AddedIds, Is.Empty);
        Assert.That(result.DeletedIds, Is.Empty);
        Assert.That(UrbanModel.Instance.Uses, Has.Count.EqualTo(10));
        Assert.That(UrbanModel.Instance.Parcels[0].Use, Is.SameAs(undefinedUse));
        Assert.That(undefinedUse.Name, Is.EqualTo(DefaultUses.UndefinedName));
        Assert.That(undefinedUse.Color, Is.EqualTo(Color.DarkGray));
    }

    [Test]
    public void Run_RemoveNonUndefinedUse_Succeeds()
    {
        // Arrange
        var residential = UrbanModel.Instance.GetUse("Residential");
        var step = new ModifyModelUsesStep();

        // Act
        var result = step.Run(UrbanModel.Instance, [new UseModificationData(UseEditType.Remove, "Residential", Color.Gold, residential.Id)]);

        // Assert
        Assert.That(UrbanModel.Instance.Uses, Has.Count.EqualTo(9));
        Assert.That(UrbanModel.Instance.Uses.Any(u => u.Id == residential.Id), Is.False);
    }

    private record ModifyModelUsesTestData(
        string CaseName,
        UseEditType EditType,
        string NewName,
        Color NewColor);

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

    private static readonly List<ModifyModelUsesTestData> _testData = BuildTestData();

    private static List<ModifyModelUsesTestData> BuildTestData()
    {
        return
        [
            new ModifyModelUsesTestData(
                "RemoveUndefined",
                UseEditType.Remove,
                DefaultUses.UndefinedName,
                Color.DarkGray),

            new ModifyModelUsesTestData(
                "EditUndefinedName",
                UseEditType.Edit,
                "Renamed",
                Color.DarkGray),

            new ModifyModelUsesTestData(
                "EditUndefinedColor",
                UseEditType.Edit,
                DefaultUses.UndefinedName,
                Color.Red),
        ];
    }
}
