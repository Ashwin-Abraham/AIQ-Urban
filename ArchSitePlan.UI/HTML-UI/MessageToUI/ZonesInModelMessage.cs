using ArchSitePlan.Core.Models;

namespace ArchSitePlan.UI.HTML_UI.MessageToUI;
public class ZonesInModelMessage : IMessageToUI
{
    public string Type { get; } 
    public string ZoneName { get; private set; }
    public List<string> Zones { get; private set; }

    public ZonesInModelMessage(string zoneName)
    {
        Type = "Zones Message";
        ZoneName = zoneName;
        Zones = CoreModel.Studies.Select(s => s.Name).ToList();
    }

    public void UpdateMessage(string newZoneName)
    {
        Zones = CoreModel.Studies.Select(s => s.Name).ToList();
        bool exists = Zones.Exists(name => name == newZoneName);

        if (exists)
            ZoneName = newZoneName;
    }
}