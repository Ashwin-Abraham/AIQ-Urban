using ArchSitePlan.Core.Models;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
public class AppStateMessage : IMessageToUI
{
    public string Type => nameof(AppStateMessage);
    public List<(string, string)> Landuses { get; set; }

    public AppStateMessage()
    {
        Landuses = AppLanduses.Uses.Select(use => use.ToStringTuple()).ToList();
    }
}