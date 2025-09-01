using ArchSitePlan.Core.Models;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
public class DirectoryPickedMessage : IMessageToUI
{
    public string Type { get; } 
    public string Directory { get; private set; }

    public DirectoryPickedMessage(
        string modelFileName = "",
        string exportPath = "")
    {
        Type = "DirectoryPickedMessage";
        Directory = exportPath;
    }
}