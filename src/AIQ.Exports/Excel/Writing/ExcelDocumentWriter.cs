using AIQ.Exports.Excel.Models;
using ClosedXML.Excel;

namespace AIQ.Exports.Excel.Writing;

public sealed class ExcelDocumentWriter
{
    private readonly IReadOnlyList<IExcelBlockWriter> _blockWriters =
    [
        new TableBlockWriter(),
    ];

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
        }

        workbook.SaveAs(absolutePath);
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
