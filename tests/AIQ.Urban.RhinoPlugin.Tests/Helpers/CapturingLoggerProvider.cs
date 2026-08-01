#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.RhinoInterface.Tests.Helpers;

internal record CapturedLogEntry(LogLevel Level, string Message);

internal class CapturingLoggerProvider : ILoggerProvider
{
    public List<CapturedLogEntry> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

    public void Dispose()
    {
    }

    private class CapturingLogger(List<CapturedLogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Add(new CapturedLogEntry(logLevel, formatter(state, exception)));
        }
    }
}
