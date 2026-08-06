using System;
using System.IO;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.RhinoInterface.Exports;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using Rhino;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public sealed class ExportExcelMessage : IMessageFromUI<ExcelExportResultMessage>
{
    private const string UnsavedProjectName = "Urban Metrics";
    private readonly IExcelSaveDialog _saveDialog;
    private readonly IUrbanMetricsExcelExporter _exporter;
    private readonly Func<string?> _documentName;
    private readonly Func<DateTime> _clock;
    private readonly UrbanModel _urbanModel;
    private readonly Action<string> _log;

    public ExportExcelMessage()
        : this(
            new RhinoExcelSaveDialog(),
            new UrbanMetricsExcelExporter(),
            () => RhinoDoc.ActiveDoc?.Name,
            () => DateTime.Now,
            UrbanModel.Instance,
            RhinoApp.WriteLine)
    {
    }

    internal ExportExcelMessage(
        IExcelSaveDialog saveDialog,
        IUrbanMetricsExcelExporter exporter,
        Func<string?> documentName,
        Func<DateTime> clock,
        UrbanModel urbanModel,
        Action<string> log)
    {
        _saveDialog = saveDialog;
        _exporter = exporter;
        _documentName = documentName;
        _clock = clock;
        _urbanModel = urbanModel;
        _log = log;
    }

    public TypeOfMessage MessageType => TypeOfMessage.ExportExcel;

    public ExcelExportResultMessage HandleMessage()
    {
        if (_urbanModel.ProjectMetrics is null)
        {
            return ExcelExportResultMessage.Failed(
                "Calculate urban metrics before exporting.");
        }

        try
        {
            var documentName = _documentName();
            var savedProjectName = GetSavedProjectName(documentName);
            var projectName = savedProjectName ?? UnsavedProjectName;
            var suggestedFileName = savedProjectName is not null
                ? GetSuggestedFileName(savedProjectName)
                : "Urban Metrics.xlsx";
            var selectedPath = _saveDialog.Show(suggestedFileName);
            if (string.IsNullOrWhiteSpace(selectedPath))
                return ExcelExportResultMessage.Cancelled();

            _exporter.Export(
                _urbanModel,
                selectedPath,
                projectName,
                _clock());
            return ExcelExportResultMessage.Succeeded(selectedPath);
        }
        catch (Exception exception)
        {
            _log($"AIQ Urban Excel export failed: {exception}");
            return ExcelExportResultMessage.Failed(
                "Excel export failed. Check the Rhino command history for details.");
        }
    }

    private static string? GetSavedProjectName(string? documentName)
    {
        if (string.IsNullOrWhiteSpace(documentName))
            return null;

        var name = Path.GetFileNameWithoutExtension(documentName).Trim();
        return name.Length == 0 ? null : name;
    }

    private static string GetSuggestedFileName(string projectName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var safeName = string.Concat(
            projectName.Select(character =>
                invalidCharacters.Contains(character) ? '_' : character));
        return $"{safeName} - Urban Metrics.xlsx";
    }
}
