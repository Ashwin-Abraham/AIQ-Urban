using Color = System.Drawing.Color;
using ArchSitePlan.Core.Models;
using ShapeCrawler;

namespace AIQ.Exports.Tests;

public class PowerpointExporterTests
{
    private static readonly List<Landuse> _testUses = new List<Landuse>()
    {
        new Landuse("Residential", Color.Yellow, Guid.NewGuid()),
        new Landuse("Shopping", Color.MediumPurple, Guid.NewGuid()),
        new Landuse("Mixed Use", Color.DarkMagenta, Guid.NewGuid()),
        new Landuse("Transport Hub", Color.FloralWhite, Guid.NewGuid()),
        new Landuse("Education", Color.Gainsboro, Guid.NewGuid()),
        new Landuse("Religious", Color.Azure, Guid.NewGuid()),
    };

    [Fact]
    public void CanExportPowerpoint()
    {
        // Arrange
        var config = new PowerpointExportConfig(
            SaveLocation: TestUtils.GetPptTargetPathInDownloads(),
            AuthorName: "Ashwin Abraham",
            ProjectName: "Oxagon Urban",
            FullProjectExportConfig: BuildZoneConfig("Full Project", TestUtils.GetTestResourcePath("TestMasterplan.jpg")),
            OtherZoneExportConfigs: [
                BuildZoneConfig("Marine District", TestUtils.GetTestResourcePath("TestMasterplan1.jpg")),
                BuildZoneConfig("Residential Cycles Zone", TestUtils.GetTestResourcePath("TestMasterplan2.jpg")),
                BuildZoneConfig("Work Exploration Zone", TestUtils.GetTestResourcePath("TestMasterplan3.jpg"))
                ]);

        // Act
        var exporter = new PowerpointExporter(config);
        bool result = exporter.ExportToPowerpoint();

        // Assert
       
    }

    [Fact]
    public void ModifyChartData()
    {
        string file = @"C:\Users\AshwinAbraham\source\repos\ArchSitePlan\AIQ.Exports\AIQ Export Resources\AIQUrbanTemplate.pptx";
        var presentation = new Presentation(file);
        var shape = presentation.Slide(3).Shape("AIQ_Chart_LandAreaUse");
        var slide = presentation.Slide(3);

        var useValues = new Dictionary<string, double>
        {
            { "Use 1", 23 },
            { "Use 2", 45 },
            { "Use 3", 56 }
        };
        slide.Shapes.AddPieChart(25, 25, 500, 300, useValues, "Land Areas");

        presentation.Save();
    }

    private ZoneExportConfig BuildZoneConfig(string zoneName, string planImagePath)
    {
        var metrics = new StudyAreaMetrics(
            boundary: null,
            zoneName: zoneName,
            parcelMetrics: GetFullProjectParcelMetrics(),
            buildingsMetrics: GetFullProjectBuildingMetrics(),
            transitMetrics: GetFullProjectTransitMetrics());

        return new ZoneExportConfig(
            Metrics: metrics,
            PlanImagePath: TestUtils.GetTestResourcePath("TestMasterplan.jpg"));
    }

    private List<TransitMetrics> GetFullProjectTransitMetrics()
    {
        return [
            new( RoadWidth: 12, RunningLength: 450, Area: 450 * 12, Id: Guid.NewGuid()),
            new( RoadWidth: 35, RunningLength: 562.456, Area: 562.456 * 35, Id: Guid.NewGuid()),
            ];
    }

    private List<BuildingMetrics> GetFullProjectBuildingMetrics()
    {
        return [
            new BuildingMetrics(Guid.NewGuid(), 235, [(_testUses[0], 235), (_testUses[1], 456), (_testUses[4], 760)]),
            new BuildingMetrics(Guid.NewGuid(), 535, [(_testUses[0], 435), (_testUses[0], 4356), (_testUses[2], 160)]),
            new BuildingMetrics(Guid.NewGuid(), 435, [(_testUses[3], 25), (_testUses[3], 346), (_testUses[2], 3360)]),
            ];
    }

    private List<ParcelMetrics> GetFullProjectParcelMetrics()
    {
        return [
            new ParcelMetrics(Guid.NewGuid(), 2343, 235, _testUses[0], 4533, 1000),
            new ParcelMetrics(Guid.NewGuid(), 2343, 235, _testUses[0], 4533, 1000),
            new ParcelMetrics(Guid.NewGuid(), 6343, 305, _testUses[2], 7533, 4000),
            new ParcelMetrics(Guid.NewGuid(), 5343, 255, _testUses[1], 6533, 4400),
            new ParcelMetrics(Guid.NewGuid(), 0, 0, _testUses[4], 0, 0),
            ];
    }
}