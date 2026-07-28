namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

public class InstallationIdMessage(string installationId) : IMessageToUI
{
    public string Type => "InstallationIdMessage";

    public string InstallationId { get; } = installationId;
}
