using System;
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
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests.Exports;

[RhinoTestFixture]
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
            () => UrbanMetricsExcelExporter.Export(
                UrbanModel.Instance,
                outputPath));

        // Assert
        Assert.That(
            exception!.Message,
            Does.Contain("Project metrics have not been calculated"));
        Assert.That(File.Exists(outputPath), Is.False);
    }

    [Test]
    public void BuildWorkbookData_CurrentMetrics_MapsProjectAndZoneScopes()
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

        // Act
        var workbookData = UrbanMetricsExcelExporter.BuildWorkbookData(
            model.ProjectMetrics!);

        // Preview
        TestContext.WriteLine(
            $"Workbook scopes: {string.Join(", ", workbookData.Worksheets.Select(x => x.Name))}");

        // Assert
        Assert.That(workbookData.Worksheets, Has.Count.EqualTo(2));
        Assert.That(workbookData.Worksheets[0].Name, Is.EqualTo("Project"));
        Assert.That(workbookData.Worksheets[1].Name, Is.EqualTo("Central/Zone"));
        Assert.That(workbookData.Worksheets[0].TotalLandArea, Is.EqualTo(100).Within(1e-6));
        Assert.That(workbookData.Worksheets[0].TotalBuiltArea, Is.EqualTo(8).Within(1e-6));
        Assert.That(workbookData.Worksheets[0].TransitCount, Is.EqualTo(2));
        Assert.That(
            workbookData.Worksheets[0].TransitMetrics.Sum(metric => metric.RunningLength),
            Is.EqualTo(30).Within(1e-6));
        Assert.That(
            workbookData.Worksheets[1].BuiltAreaByUse.Single().UseName,
            Is.EqualTo("Residential"));
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
