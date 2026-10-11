#nullable enable

using System;
using System.IO;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.RhinoInterface.Exports;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;
using NUnit.Framework;

namespace AIQ.Urban.RhinoInterface.Tests;

[TestFixture]
public class ExportExcelMessageTests
{
    private readonly UrbanModel _urbanModel = UrbanModel.Instance;

    [SetUp]
    public void SetUp()
    {
        _urbanModel.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        _urbanModel.Reset();
    }

    [Test]
    public void HandleMessage_WhenSaveIsCancelled_ReturnsCancelledWithoutExporting()
    {
        _urbanModel.ProjectMetrics = new ProjectMetrics([], [], [], []);
        var dialog = new FakeExcelSaveDialog(null);
        var exporter = new FakeUrbanMetricsExcelExporter();
        var message = CreateMessage(dialog, exporter);

        var result = message.HandleMessage();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Cancelled"));
            Assert.That(result.FilePath, Is.Null);
            Assert.That(exporter.CallCount, Is.Zero);
        });
    }

    [Test]
    public void HandleMessage_WhenMetricsExist_ExportsToSelectedPath()
    {
        const string selectedPath = @"C:\Exports\Sample Project - Urban Metrics.xlsx";
        var exportedAt = new DateTime(2026, 8, 4, 14, 30, 0);
        _urbanModel.ProjectMetrics = new ProjectMetrics([], [], [], []);
        var dialog = new FakeExcelSaveDialog(selectedPath);
        var exporter = new FakeUrbanMetricsExcelExporter();
        var message = new ExportExcelMessage(
            dialog,
            exporter,
            () => "Sample Project.3dm",
            () => exportedAt,
            _urbanModel,
            _ => { });

        var result = message.HandleMessage();

        Assert.Multiple(() =>
        {
            Assert.That(dialog.SuggestedFileName, Is.EqualTo("Sample Project - Urban Metrics.xlsx"));
            Assert.That(exporter.CallCount, Is.EqualTo(1));
            Assert.That(exporter.FilePath, Is.EqualTo(selectedPath));
            Assert.That(exporter.ProjectName, Is.EqualTo("Sample Project"));
            Assert.That(exporter.ExportedAt, Is.EqualTo(exportedAt));
            Assert.That(result.Status, Is.EqualTo("Succeeded"));
            Assert.That(result.FilePath, Is.EqualTo(selectedPath));
        });
    }

    [Test]
    public void HandleMessage_WhenMetricsAreMissing_ReturnsFailureWithoutShowingDialog()
    {
        var dialog = new FakeExcelSaveDialog(@"C:\Exports\Ignored.xlsx");
        var exporter = new FakeUrbanMetricsExcelExporter();
        var message = CreateMessage(dialog, exporter);

        var result = message.HandleMessage();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Failed"));
            Assert.That(result.ErrorMessage, Is.EqualTo("Calculate urban metrics before exporting."));
            Assert.That(dialog.CallCount, Is.Zero);
            Assert.That(exporter.CallCount, Is.Zero);
        });
    }

    [Test]
    public void HandleMessage_WhenExporterThrows_ReturnsConciseFailure()
    {
        _urbanModel.ProjectMetrics = new ProjectMetrics([], [], [], []);
        var dialog = new FakeExcelSaveDialog(@"C:\Exports\Report.xlsx");
        var exporter = new FakeUrbanMetricsExcelExporter
        {
            ExceptionToThrow = new IOException("Disk details that should not reach the UI")
        };
        var message = CreateMessage(dialog, exporter);

        var result = message.HandleMessage();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Failed"));
            Assert.That(result.ErrorMessage, Is.EqualTo("Excel export failed. Check the Rhino command history for details."));
            Assert.That(result.ErrorMessage, Does.Not.Contain("Disk details"));
        });
    }

    [Test]
    public void HandleMessage_WhenSaveDialogThrows_ReturnsConciseFailure()
    {
        _urbanModel.ProjectMetrics = new ProjectMetrics([], [], [], []);
        var dialog = new FakeExcelSaveDialog(null)
        {
            ExceptionToThrow = new InvalidOperationException("Native dialog details")
        };
        var exporter = new FakeUrbanMetricsExcelExporter();
        var message = CreateMessage(dialog, exporter);

        var result = message.HandleMessage();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Failed"));
            Assert.That(result.ErrorMessage, Is.EqualTo("Excel export failed. Check the Rhino command history for details."));
            Assert.That(exporter.CallCount, Is.Zero);
        });
    }

    [Test]
    public void HandleMessage_WhenProjectIsNamedUrbanModel_UsesProjectInSuggestedFileName()
    {
        _urbanModel.ProjectMetrics = new ProjectMetrics([], [], [], []);
        var dialog = new FakeExcelSaveDialog(null);
        var exporter = new FakeUrbanMetricsExcelExporter();
        var message = new ExportExcelMessage(
            dialog,
            exporter,
            () => "Urban Model.3dm",
            () => new DateTime(2026, 8, 4, 14, 30, 0),
            _urbanModel,
            _ => { });

        message.HandleMessage();

        Assert.That(
            dialog.SuggestedFileName,
            Is.EqualTo("Urban Model - Urban Metrics.xlsx"));
    }

    [Test]
    public void HandleMessage_WithFilePath_ExportsWithoutShowingDialog()
    {
        const string path = @"C:\Exports\Scripted.xlsx";
        _urbanModel.ProjectMetrics = new ProjectMetrics([], [], [], []);
        var dialog = new FakeExcelSaveDialog(null);
        var exporter = new FakeUrbanMetricsExcelExporter();
        var message = CreateMessage(dialog, exporter, path);

        var result = message.HandleMessage();

        Assert.That(dialog.CallCount, Is.Zero);
        Assert.That(exporter.FilePath, Is.EqualTo(path));
        Assert.That(result.Status, Is.EqualTo("Succeeded"));
    }

    [Test]
    public void HandleMessage_WithEmptyFilePath_ReturnsFailureWithoutShowingDialog()
    {
        _urbanModel.ProjectMetrics = new ProjectMetrics([], [], [], []);
        var dialog = new FakeExcelSaveDialog(null);
        var exporter = new FakeUrbanMetricsExcelExporter();
        var message = CreateMessage(dialog, exporter, "");

        var result = message.HandleMessage();

        Assert.That(dialog.CallCount, Is.Zero);
        Assert.That(exporter.CallCount, Is.Zero);
        Assert.That(result.Status, Is.EqualTo("Failed"));
    }

    private ExportExcelMessage CreateMessage(
        IExcelSaveDialog dialog,
        IUrbanMetricsExcelExporter exporter,
        string? filePath = null)
    {
        return new ExportExcelMessage(
            dialog,
            exporter,
            () => null,
            () => new DateTime(2026, 8, 4, 14, 30, 0),
            _urbanModel,
            _ => { }) { FilePath = filePath };
    }

    private sealed class FakeExcelSaveDialog(string? selectedPath) : IExcelSaveDialog
    {
        public int CallCount { get; private set; }
        public string? SuggestedFileName { get; private set; }
        public Exception? ExceptionToThrow { get; init; }

        public string? Show(string suggestedFileName)
        {
            CallCount++;
            SuggestedFileName = suggestedFileName;
            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;
            return selectedPath;
        }
    }

    private sealed class FakeUrbanMetricsExcelExporter : IUrbanMetricsExcelExporter
    {
        public int CallCount { get; private set; }
        public string? FilePath { get; private set; }
        public string? ProjectName { get; private set; }
        public DateTime? ExportedAt { get; private set; }
        public Exception? ExceptionToThrow { get; init; }

        public void Export(
            UrbanModel urbanModel,
            string filePath,
            string projectName,
            DateTime exportedAt)
        {
            CallCount++;
            FilePath = filePath;
            ProjectName = projectName;
            ExportedAt = exportedAt;

            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;
        }
    }
}
