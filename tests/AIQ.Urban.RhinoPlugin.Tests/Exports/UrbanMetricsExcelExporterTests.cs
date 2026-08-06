using System;
using System.Drawing;
using System.IO;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Exports;
using NUnit.Framework;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Tests.Exports;

[TestFixture]
public class UrbanMetricsExcelExporterTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public void Export_MissingMetrics_ThrowsClearException()
    {
        // Arrange
        var outputPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}-metrics.xlsx");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => new UrbanMetricsExcelExporter().Export(
                UrbanModel.Instance,
                outputPath,
                "Unsaved Project",
                new DateTime(2026, 8, 4, 14, 30, 0)));

        // Assert
        Assert.That(
            exception!.Message,
            Does.Contain("Project metrics have not been calculated"));
        Assert.That(File.Exists(outputPath), Is.False);
    }

    [Test]
    public void BuildWorkbookData_CurrentMetrics_MapsAcceptedProjectAndZoneInputs()
    {
        // Arrange
        var model = UrbanModel.Instance;
        var residential = model.GetUse("Residential");
        var building = new Building(
            CreatePolygon(2, 2, 2),
            typicalFloorHeight: 3,
            floorCount: 2,
            residential);
        var parcel = new Parcel(
            new PolyCurve2D(CreatePolygon(0, 0, 10).Geometry),
            residential);
        var firstTransit = CreateTransit(
            0, 5, 10, 5, width: 6, TransitType.Road);
        var secondTransit = CreateTransit(
            0, 6, 20, 6, width: 6, TransitType.Road);
        var zone = new Zone(
            CreatePolygon(-1, -1, 30),
            "Central/Zone");

        model.Buildings.Add(building);
        model.Parcels.Add(parcel);
        model.Transits.AddRange([firstTransit, secondTransit]);
        model.Zones.Add(zone);
        ProjectMetricsCalculator.CalculateAndStore(
            model,
            containmentTolerance: 0.001);
        var transitColor = Color.FromArgb(80, 90, 100);
        firstTransit.Color = transitColor;
        secondTransit.Color = transitColor;
        var exportedAt = new DateTime(2026, 8, 4, 14, 30, 0);

        // Act
        var workbookData = UrbanMetricsExcelExporter.BuildWorkbookData(
            model,
            "Sample Project",
            exportedAt);

        // Preview
        TestContext.WriteLine(
            $"Project: {workbookData.ProjectName}; Zones: {workbookData.Zones.Count}");

        // Assert
        Assert.That(workbookData.ProjectName, Is.EqualTo("Sample Project"));
        Assert.That(workbookData.ExportedAt, Is.EqualTo(exportedAt));
        Assert.That(workbookData.Project.ParcelUses.Single().ParcelCount, Is.EqualTo(1));
        Assert.That(workbookData.Project.ParcelUses.Single().Area, Is.EqualTo(100).Within(1e-6));
        Assert.That(
            workbookData.Project.ParcelUses.Single().SumOfParcelPerimeters,
            Is.EqualTo(40).Within(1e-6));
        Assert.That(
            workbookData.Project.ParcelUses.Single().BuildingFootprint,
            Is.EqualTo(4).Within(1e-6));
        Assert.That(
            workbookData.Project.BuiltPlateUses.Single().BuiltArea,
            Is.EqualTo(8).Within(1e-6));
        Assert.That(
            workbookData.Project.TransitMetrics.All(row => row.Color == transitColor),
            Is.True);
        Assert.That(workbookData.Zones, Has.Count.EqualTo(1));
        Assert.That(
            workbookData.Zones.Single().ZoneBoundaryArea,
            Is.EqualTo(900).Within(1e-6));
        Assert.That(
            workbookData.Zones.Single().ParcelArea,
            Is.EqualTo(100).Within(1e-6));
    }

    private static Transit CreateTransit(
        double fromX,
        double fromY,
        double toX,
        double toY,
        double width,
        TransitType type)
    {
        return new Transit(
            new Curve2D(
                new LineCurve(
                    new Point3d(fromX, fromY, 0),
                    new Point3d(toX, toY, 0)),
                Guid.NewGuid()),
            width,
            type);
    }

    private static Polygon2D CreatePolygon(
        double x,
        double y,
        double size)
    {
        return new Polygon2D(new PolylineCurve([
            new Point3d(x, y, 0),
            new Point3d(x + size, y, 0),
            new Point3d(x + size, y + size, 0),
            new Point3d(x, y + size, 0),
            new Point3d(x, y, 0),
        ]));
    }
}
