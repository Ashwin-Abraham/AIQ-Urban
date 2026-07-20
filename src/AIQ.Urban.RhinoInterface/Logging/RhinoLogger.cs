using Microsoft.Extensions.Logging;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Logging;

internal class RhinoLogger(string categoryName) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var levelTag = logLevel switch
        {
            LogLevel.Warning => "[Warn]",
            LogLevel.Error => "[Error]",
            LogLevel.Critical => "[Error]",
            _ => "[Info]"
        };

        var message = formatter(state, exception);
        var output = exception is not null
            ? $"{levelTag} {categoryName}: {message} — {exception.Message}"
            : $"{levelTag} {categoryName}: {message}";

        RhinoApp.WriteLine(output);
    }
}
