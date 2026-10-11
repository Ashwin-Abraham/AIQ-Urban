using System;
using System.Drawing;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests;

[RhinoTestFixture]
public class MessageHandlersTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public void AddParcelBoundaryMessage_WithSelectedCurve_AddsManagedBoundaryAndReturnsModelStateMessage()
    {
        // Arrange
        var doc = RhinoDoc.ActiveDoc ?? throw new InvalidOperationException("No active Rhino document.");
        var sourceId = doc.Objects.AddCurve(new LineCurve(
            new Point3d(0, 5, 0),
            new Point3d(10, 5, 0)));
        doc.Objects.Find(sourceId)?.Select(true);

        var message = new AddParcelBoundaryMessage();

        // Act
        var result = message.HandleMessage();

        // Preview
        TestContext.WriteLine($"Managed boundary count: {UrbanModel.Instance.ParcelBoundaries.Count}");

        // Assert
        Assert.That(result, Is.TypeOf<ModelStateMessage>());
        Assert.That(UrbanModel.Instance.ParcelBoundaries, Has.Count.EqualTo(1));
        Assert.That(UrbanModel.Instance.ParcelBoundaries[0].Id, Is.Not.EqualTo(sourceId));
        Assert.That(doc.Objects.FindId(sourceId), Is.Null);
        Assert.That(doc.Objects.FindId(UrbanModel.Instance.ParcelBoundaries[0].Id), Is.Not.Null);
    }

    [Test]
    public void AddZoneMessage_HandleMessage_AddsZoneAndReturnsModelStateMessage()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var curveId = doc.Objects.AddCurve(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        doc.Objects.Find(curveId)?.Select(true);

        var message = new AddZoneMessage { Name = "Downtown" };

        var result = message.HandleMessage();

        Assert.That(result, Is.TypeOf<ModelStateMessage>());
        Assert.That(UrbanModel.Instance.Zones, Has.Count.EqualTo(1));
        Assert.That(UrbanModel.Instance.Zones[0].Name, Is.EqualTo("Downtown"));
    }

    [Test]
    public void ChangeUseMessage_WithParcelSelected_ChangesParcelUseWithoutRebakingTransitSurface()
    {
        var doc = RhinoDoc.ActiveDoc ?? throw new InvalidOperationException("No active Rhino document.");
        var parcel = AddParcelToDocAndModel(doc, DefaultUses.Create("Public"));
        var transitSurface = AddTransitSurfaceToDocAndModel(doc);
        var originalTransitSurfaceId = transitSurface.Id;

        doc.Objects.Find(parcel.Outline.Id)?.Select(true);

        var message = new ChangeUseMessage { Use = "Residential" };

        var result = message.HandleMessage();

        Assert.That(result, Is.TypeOf<ModelStateMessage>());
        Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(1));
        Assert.That(UrbanModel.Instance.Parcels[0].Use.Name, Is.EqualTo("Residential"));
        Assert.That(transitSurface.Id, Is.EqualTo(originalTransitSurfaceId));
    }

    [Test]
    public void ChangeUseMessage_WithBuildingSelected_ChangesBuildingUseAndReturnsModelStateMessage()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var building = AddBuildingToDocAndModel(doc, DefaultUses.Create("Residential"));

        doc.Objects.Find(building.Envelope.Id)?.Select(true);

        var message = new ChangeUseMessage { Use = "Commercial" };

        var result = message.HandleMessage();

        Assert.That(result, Is.TypeOf<ModelStateMessage>());
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(1));
        Assert.That(UrbanModel.Instance.Buildings[0].FloorPlates, Is.Not.Empty);
        Assert.That(UrbanModel.Instance.Buildings[0].FloorPlates.All(p => p.Use.Name == "Commercial"), Is.True);
    }

    [Test]
    public void AddParcelBoundaryMessage_WithObjectIds_UsesSpecifiedCurve()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var sourceId = doc.Objects.AddCurve(new LineCurve(new Point3d(0, 5, 0), new Point3d(10, 5, 0)));

        new AddParcelBoundaryMessage { ObjectIds = [sourceId] }.HandleMessage();

        Assert.That(UrbanModel.Instance.ParcelBoundaries, Has.Count.EqualTo(1));
        Assert.That(doc.Objects.FindId(sourceId), Is.Null);
    }

    [Test]
    public void AddZoneMessage_WithObjectIds_IgnoresOtherSelectedCurves()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var sourceId = doc.Objects.AddCurve(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0), new Point3d(10, 10, 0),
            new Point3d(0, 10, 0), new Point3d(0, 0, 0),
        ]));
        var otherId = doc.Objects.AddCurve(new LineCurve(new Point3d(20, 0, 0), new Point3d(30, 0, 0)));
        doc.Objects.FindId(otherId)?.Select(true);

        new AddZoneMessage { Name = "Downtown", ObjectIds = [sourceId] }.HandleMessage();

        Assert.That(UrbanModel.Instance.Zones, Has.Count.EqualTo(1));
        Assert.That(doc.Objects.FindId(otherId), Is.Not.Null);
    }

    [Test]
    public void TransitMessage_WithObjectIds_UsesSpecifiedCurve()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var sourceId = doc.Objects.AddCurve(new LineCurve(new Point3d(0, 0, 0), new Point3d(20, 0, 0)));

        new TransitMessage { RoadWidth = 4, ObjectIds = [sourceId] }.HandleMessage();

        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));
        Assert.That(doc.Objects.FindId(sourceId), Is.Null);
    }

    [Test]
    public void BuildingMessage_WithObjectIds_UsesSpecifiedFootprint()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var sourceId = doc.Objects.AddCurve(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0), new Point3d(10, 10, 0),
            new Point3d(0, 10, 0), new Point3d(0, 0, 0),
        ]));

        new BuildingMessage { FloorHeight = 3, FloorCount = 2, ObjectIds = [sourceId] }.HandleMessage();

        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(1));
        Assert.That(doc.Objects.FindId(sourceId), Is.Null);
    }

    [Test]
    public void ChangeUseMessage_WithObjectIds_UsesSpecifiedParcel()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var parcel = AddParcelToDocAndModel(doc, DefaultUses.Create("Public"));

        new ChangeUseMessage { Use = "Residential", ObjectIds = [parcel.Outline.Id] }.HandleMessage();

        Assert.That(UrbanModel.Instance.Parcels[0].Use.Name, Is.EqualTo("Residential"));
    }

    [Test]
    public void AddZoneMessage_WithMissingObjectId_RejectsBeforeEditing()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var message = new AddZoneMessage { Name = "Downtown", ObjectIds = [Guid.NewGuid()] };

        Assert.That(() => message.HandleMessage(), Throws.ArgumentException.With.Message.Contains("was not found"));
        Assert.That(UrbanModel.Instance.Zones, Is.Empty);
    }

    [Test]
    public void AddZoneMessage_WithEmptyObjectIds_UsesSelection()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var selectedId = doc.Objects.AddCurve(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0), new Point3d(10, 10, 0),
            new Point3d(0, 10, 0), new Point3d(0, 0, 0),
        ]));
        doc.Objects.FindId(selectedId)?.Select(true);
        var message = new AddZoneMessage { Name = "Downtown", ObjectIds = [] };

        message.HandleMessage();

        Assert.That(UrbanModel.Instance.Zones, Has.Count.EqualTo(1));
    }

    private static Parcel AddParcelToDocAndModel(RhinoDoc doc, Use use)
    {
        var polygon = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));

        var parcel = new Parcel(polygon.ToPolyCurve2d(), use);
        var curveId = doc.Objects.AddCurve(polygon.Geometry);
        parcel.Outline.Id = curveId;
        UrbanModel.Instance.Parcels.Add(parcel);

        return parcel;
    }

    private static Building AddBuildingToDocAndModel(RhinoDoc doc, Use use)
    {
        var footprint = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));

        var building = new AIQ.Urban.Core.Models.UrbanElements.Building(footprint, 3.5, 2, use);
        var envelopeId = doc.Objects.AddBrep(building.Envelope.Geometry);
        building.Envelope.Id = envelopeId;

        foreach (var floorPlate in building.FloorPlates)
        {
            var curveId = doc.Objects.AddCurve(floorPlate.Outline.Geometry);
            floorPlate.Outline.Id = curveId;
        }

        UrbanModel.Instance.Buildings.Add(building);

        return building;
    }

    private static Profile2D AddTransitSurfaceToDocAndModel(RhinoDoc doc)
    {
        var polygon = new Polygon2D(new PolylineCurve([
            new Point3d(0, 20, 0),
            new Point3d(20, 20, 0),
            new Point3d(20, 25, 0),
            new Point3d(0, 25, 0),
            new Point3d(0, 20, 0),
        ]));

        var profile = new Profile2D(polygon.ToPolyCurve2d(), []);
        profile.Id = doc.Objects.AddBrep(profile.Geometry);
        UrbanModel.Instance.TransitSurfaces.Add((profile, TransitType.Road));

        return profile;
    }

    [TestCase("#6d2222", 109, 34, 34)]
    [TestCase("#FF0000", 255, 0, 0)]
    [TestCase("#00FF00", 0, 255, 0)]
    [TestCase("#0000FF", 0, 0, 255)]
    public void ParseColor_HexString_ReturnsCorrectColor(string hex, int r, int g, int b)
    {
        var color = LanduseListEditMessage.ParseColor(hex);
        Assert.That(color.R, Is.EqualTo(r));
        Assert.That(color.G, Is.EqualTo(g));
        Assert.That(color.B, Is.EqualTo(b));
    }

    [Test]
    public void ParseColor_EightDigitHex_ReturnsWithAlpha()
    {
        var color = LanduseListEditMessage.ParseColor("#80FF0000");
        Assert.That(color.A, Is.EqualTo(128));
        Assert.That(color.R, Is.EqualTo(255));
        Assert.That(color.G, Is.EqualTo(0));
        Assert.That(color.B, Is.EqualTo(0));
    }

    [Test]
    public void ParseColor_CommaSeparatedRgb_ReturnsCorrectColor()
    {
        var color = LanduseListEditMessage.ParseColor("255,128,0");
        Assert.That(color, Is.EqualTo(Color.FromArgb(255, 128, 0)));
    }

    [Test]
    public void ParseColor_NamedColor_ReturnsCorrectColor()
    {
        var color = LanduseListEditMessage.ParseColor("Red");
        Assert.That(color, Is.EqualTo(Color.Red));
    }

    [Test]
    public void ParseColor_EmptyString_ReturnsGray()
    {
        var color = LanduseListEditMessage.ParseColor("");
        Assert.That(color, Is.EqualTo(Color.Gray));
    }

    [Test]
    public void ParseColor_Null_ReturnsGray()
    {
        var color = LanduseListEditMessage.ParseColor(null);
        Assert.That(color, Is.EqualTo(Color.Gray));
    }
}
