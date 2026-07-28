using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using Rhino;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public class GetInstallationIdMessage : IMessageFromUI<InstallationIdMessage>
{
    private readonly InstallationIdProvider _provider;

    public TypeOfMessage MessageType => TypeOfMessage.GetInstallationId;

    public GetInstallationIdMessage()
        : this(new InstallationIdProvider(InstallationIdProvider.DefaultDirectoryPath))
    {
    }

    internal GetInstallationIdMessage(InstallationIdProvider provider)
    {
        _provider = provider;
    }

    public InstallationIdMessage HandleMessage()
    {
        var installationId = _provider.GetOrCreate(out var warning);

        if (warning != string.Empty)
            RhinoApp.WriteLine($"AIQ Urban UI: {warning}");

        return new InstallationIdMessage(installationId);
    }
}
