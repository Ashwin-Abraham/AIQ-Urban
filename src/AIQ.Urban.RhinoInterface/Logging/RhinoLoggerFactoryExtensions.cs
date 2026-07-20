using Microsoft.Extensions.Logging;

namespace AIQ.Urban.RhinoInterface.Logging;

public static class RhinoLoggerFactoryExtensions
{
    public static ILoggerFactory AddRhinoLog(this LoggerFactory factory)
    {
        factory.AddProvider(new RhinoLogProvider());
        return factory;
    }
}
