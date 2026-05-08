using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Newtonsoft.Json;
using Rhino.UI;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;

public class PickFileMessage : IMessageFromUI<FilePickedMessage>
{
    public TypeOfMessage MessageType { get; init; } = TypeOfMessage.PickFile;

    [JsonProperty("fileType")]
    public string FileType { get; init; }

    public FilePickedMessage HandleMessage()
    {
        // Create a new OpenFileDialog instance.
        OpenFileDialog openFileDialog = new OpenFileDialog();

        // Set optional properties (e.g., file filters).
        openFileDialog.Filter = FileType;
        openFileDialog.Title = $"Select a {FileType} File";

        bool success = openFileDialog.ShowOpenDialog();
        string filePath = success? openFileDialog.FileName:"none";
        string errorMessage = success ? "Picked a file" : "Failed to pick a file";

        return new FilePickedMessage(filePath, success, errorMessage);
    }
}
