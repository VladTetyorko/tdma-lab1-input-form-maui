using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// A logger that records what was logged, for tests that assert levels and messages.
// Several loggers can share one list, so a test sees the calls of every class involved in a flow.
public sealed class CapturingLogger<T>(List<LogRecord>? sink = null) : ILogger<T>
{
    public List<LogRecord> Records { get; } = sink ?? [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Records.Add(new LogRecord(logLevel, typeof(T).Name, formatter(state, exception), exception));
}
