using System.Linq;
using AIQ.Urban.Core;
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
    public void ChangeParcelUseMessage_HandleMessage_ChangesParcelUseAndReturnsModelStateMessage()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var parcel = AddParcelToDocAndModel(doc, DefaultUses.Public);

        doc.Objects.Find(parcel.Outline.Id)?.Select(true);

        var message = new ChangeUseMessage { Use = "Residential" };

        var result = message.HandleMessage();

        Assert.That(result, Is.TypeOf<ModelStateMessage>());
        Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(1));
        Assert.That(UrbanModel.Instance.Parcels[0].Use.Name, Is.EqualTo("Residential"));
    }

    [Test]
    public void ChangeUseMessage_WithBuildingSelected_ChangesBuildingUseAndReturnsModelStateMessage()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var building = AddBuildingToDocAndModel(doc, DefaultUses.Residential);

        doc.Objects.Find(building.Envelope.Id)?.Select(true);

        var message = new ChangeUseMessage { Use = "Commercial" };

        var result = message.HandleMessage();

        Assert.That(result, Is.TypeOf<ModelStateMessage>());
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(1));
        Assert.That(UrbanModel.Instance.Buildings[0].FloorPlates, Is.Not.Empty);
        Assert.That(UrbanModel.Instance.Buildings[0].FloorPlates.All(p => p.Use.Name == "Commercial"), Is.True);
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

        var parcel = new Parcel(polygon, use);
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
}
