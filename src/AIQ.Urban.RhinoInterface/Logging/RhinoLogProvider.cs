using Microsoft.Extensions.Logging;

namespace AIQ.Urban.RhinoInterface.Logging;

internal class RhinoLogProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new RhinoLogger(categoryName);

    public void Dispose() { }
}
