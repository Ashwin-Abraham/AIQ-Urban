namespace ArchSitePlan.UI.HTML_UI.MessageToUI;

public class FilePickedMessage(
    string filePath, 
    bool success, 
    string errorMessage) : IMessageToUI
{
    public string Type { get; } = nameof(FilePickedMessage);

    public string FilePath { get; set; } = filePath;
    public bool Success { get; set; } = success;
    public string ErrorMessage { get; set; } = errorMessage;
}
