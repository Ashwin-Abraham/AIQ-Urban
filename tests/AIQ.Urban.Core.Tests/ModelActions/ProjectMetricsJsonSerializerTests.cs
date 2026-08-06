using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models.Metrics;
using NUnit.Framework;
using System;
using System.IO;

namespace AIQ.Urban.Core.Tests.ModelActions;

public class ProjectMetricsJsonSerializerTests
{
    [Test]
    public void Save_WritesFile_WithValidJson()
    {
        var metrics = new ProjectMetrics([], []);
        var filePath = Path.Combine(Path.GetTempPath(), $"test_metrics_{Guid.NewGuid()}.json");

        try
        {
            ProjectMetricsJsonSerializer.Save(metrics, filePath);

            Assert.That(File.Exists(filePath), Is.True);
            var content = File.ReadAllText(filePath);
            Assert.That(content, Is.Not.Empty);

            Assert.That(content, Does.Contain("totalBuiltUpArea"));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Test]
    public void Save_CreatesDirectory_WhenItDoesNotExist()
    {
        var metrics = new ProjectMetrics([], []);
        var dir = Path.Combine(Path.GetTempPath(), $"test_dir_{Guid.NewGuid()}");
        var filePath = Path.Combine(dir, "metrics.json");

        try
        {
            ProjectMetricsJsonSerializer.Save(metrics, filePath);

            Assert.That(Directory.Exists(dir), Is.True);
            Assert.That(File.Exists(filePath), Is.True);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public void Save_WritesIndentedJson()
    {
        var metrics = new ProjectMetrics([], []);
        var filePath = Path.Combine(Path.GetTempPath(), $"test_metrics_{Guid.NewGuid()}.json");

        try
        {
            ProjectMetricsJsonSerializer.Save(metrics, filePath);

            var content = File.ReadAllText(filePath);
            Assert.That(content.Contains('\n') || content.Contains("\r\n"), Is.True);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Test]
    public void Save_DoesNotSerializeZoneParcelExportInputs()
    {
        var zone = new ZoneMetrics(
            Guid.NewGuid(),
            "Zone",
            100,
            buildings: [],
            parcels: [],
            transits: []);
        var metrics = new ProjectMetrics([], [], [], [zone]);
        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"test_metrics_{Guid.NewGuid()}.json");

        try
        {
            ProjectMetricsJsonSerializer.Save(metrics, filePath);

            var content = File.ReadAllText(filePath);
            Assert.That(content, Does.Not.Contain("ParcelMetrics"));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
