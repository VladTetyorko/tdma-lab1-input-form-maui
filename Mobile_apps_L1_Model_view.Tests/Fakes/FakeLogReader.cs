using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeLogReader : ILogReader
{
    // Newest first, as the real reader returns them
    public List<LogEntry> Entries { get; } = [];

    public List<LogLevel> RequestedLevels { get; } = [];

    public Exception? Failure { get; set; }

    public Task<IReadOnlyList<LogEntry>> ReadNewestAsync(LogLevel minimumLevel, int maxEntries = ILogReader.DefaultMaxEntries)
    {
        RequestedLevels.Add(minimumLevel);
        if (Failure is not null)
        {
            return Task.FromException<IReadOnlyList<LogEntry>>(Failure);
        }

        IReadOnlyList<LogEntry> result = Entries.Where(e => e.Level >= minimumLevel).Take(maxEntries).ToList();
        return Task.FromResult(result);
    }
}
