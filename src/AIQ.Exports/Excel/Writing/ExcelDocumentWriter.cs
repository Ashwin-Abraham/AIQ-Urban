using AIQ.Exports.Excel.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AIQ.Exports.Excel.Writing;

public sealed class ExcelDocumentWriter
{
    private readonly IReadOnlyList<IExcelBlockWriter> _blockWriters =
    [
        new TableBlockWriter(),
    ];

    private readonly Action<string> _postProcess;

    public ExcelDocumentWriter()
        : this(EnsureFillFormatsApplied)
    {
    }

    internal ExcelDocumentWriter(Action<string> postProcess)
    {
        ArgumentNullException.ThrowIfNull(postProcess);
        _postProcess = postProcess;
    }

    public void Write(
        ExcelDocument document,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException(
                "A destination file path is required.",
                nameof(filePath));

        if (document.Worksheets.Count == 0)
            throw new ArgumentException(
                "An Excel document must contain at least one worksheet.",
                nameof(document));

        var absolutePath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(absolutePath)!;
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(
                $"The destination directory does not exist: {directory}");

        ValidateWorksheetNames(document.Worksheets);

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(absolutePath)}.{Guid.NewGuid():N}.tmp.xlsx");

        try
        {
            WriteWorkbook(document, temporaryPath);

            if (File.Exists(absolutePath))
                File.Replace(temporaryPath, absolutePath, destinationBackupFileName: null);
            else
                File.Move(temporaryPath, absolutePath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private void WriteWorkbook(
        ExcelDocument document,
        string filePath)
    {

        using var workbook = new XLWorkbook();

        foreach (var definition in document.Worksheets)
        {
            var worksheet = workbook.AddWorksheet(definition.Name);
            definition.PlacementStrategy.Place(
                definition.Blocks,
                definition.StartingAddress);

            foreach (var block in definition.Blocks)
            {
                var writer = _blockWriters.FirstOrDefault(
                    candidate => candidate.CanWrite(block));
                if (writer is null)
                {
                    throw new NotSupportedException(
                        $"No writer is registered for blocks of type "
                        + $"{block.GetType().FullName}.");
                }

                writer.Write(worksheet, block);
            }

            worksheet.ColumnsUsed().AdjustToContents();
            ApplyWorksheetOptions(worksheet, definition.Options);
        }

        workbook.RecalculateAllFormulas();
        workbook.SaveAs(filePath);
        _postProcess(filePath);
    }

    private static void EnsureFillFormatsApplied(string filePath)
    {
        using var document = SpreadsheetDocument.Open(filePath, true);
        var stylesheet = document.WorkbookPart?
            .WorkbookStylesPart?
            .Stylesheet;
        var cellFormats = stylesheet?.CellFormats;

        if (stylesheet is null || cellFormats is null)
            return;

        foreach (var format in cellFormats.Elements<CellFormat>())
        {
            if (format.FillId?.Value > 0)
                format.ApplyFill = true;
        }

        stylesheet.Save();
    }

    private static void ApplyWorksheetOptions(
        IXLWorksheet worksheet,
        ExcelWorksheetOptions? options)
    {
        if (options is null)
            return;

        worksheet.ShowGridLines = !options.HideGridlines;

        if (options.FrozenRowCount > 0)
            worksheet.SheetView.FreezeRows(options.FrozenRowCount);

        if (options.ColumnWidths is null)
            return;

        foreach (var (column, width) in options.ColumnWidths)
            worksheet.Column(column).Width = width;
    }

    private static void ValidateWorksheetNames(
        IReadOnlyList<ExcelWorksheetDefinition> worksheets)
    {
        if (worksheets.Any(worksheet =>
                string.IsNullOrWhiteSpace(worksheet.Name)))
        {
            throw new ArgumentException(
                "Worksheet names cannot be null or whitespace.",
                nameof(worksheets));
        }

        var duplicate = worksheets
            .GroupBy(worksheet => worksheet.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"The worksheet name '{duplicate.Key}' is duplicated.",
                nameof(worksheets));
        }
    }
}
