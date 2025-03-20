using ArchSitePlan.Core.Models;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
public class ExportLocationMessage : IMessageToUI
{
    public string Type { get; } 
    public string ModelFileName { get; private set; }
    public string ExportPath { get; private set; }

    public ExportLocationMessage(
        string modelFileName = "",
        string exportPath = "")
    {
        Type = "Export Location Message";
        ModelFileName = modelFileName;
        ExportPath = exportPath;
    }

    public void UpdateMessage(string newZoneName)
    {
        throw new NotImplementedException();
    }
}