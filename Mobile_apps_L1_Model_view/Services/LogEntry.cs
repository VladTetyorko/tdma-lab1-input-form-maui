using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

// One parsed log record. Exception holds the stack trace lines that follow the header line (null when none)
public sealed record LogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Source,
    string Message,
    string? Exception = null)
{
    // "StockService" instead of "Mobile_apps_L1_Model_view.Services.StockService"
    public string ShortSource => Source[(Source.LastIndexOf('.') + 1)..];

    public bool HasException => Exception is not null;

    // Three-letter code of the log format, for the list ("INF", "WRN", ...)
    public string LevelCode => LogLineParser.CodeOf(Level);
}
