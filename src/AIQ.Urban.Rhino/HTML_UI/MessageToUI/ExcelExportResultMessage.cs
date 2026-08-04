namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

public sealed class ExcelExportResultMessage : IMessageToUI
{
    private ExcelExportResultMessage(
        string status,
        string? filePath,
        string? errorMessage)
    {
        Status = status;
        FilePath = filePath;
        ErrorMessage = errorMessage;
    }

    public string Type => "ExcelExportResultMessage";

    public string Status { get; }

    public string? FilePath { get; }

    public string? ErrorMessage { get; }

    public static ExcelExportResultMessage Succeeded(string filePath)
    {
        return new("Succeeded", filePath, null);
    }

    public static ExcelExportResultMessage Cancelled()
    {
        return new("Cancelled", null, null);
    }

    public static ExcelExportResultMessage Failed(string errorMessage)
    {
        return new("Failed", null, errorMessage);
    }
}
