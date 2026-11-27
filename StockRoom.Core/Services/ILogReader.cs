using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

// Reads the app's own log files back for the in-app log viewer
public interface ILogReader
{
    public const int DefaultMaxEntries = 200;

    // The newest entries at or above minimumLevel, newest first, at most maxEntries. Empty when there is no log yet
    Task<IReadOnlyList<LogEntry>> ReadNewestAsync(LogLevel minimumLevel, int maxEntries = DefaultMaxEntries);
}
