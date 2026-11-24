using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

// Files are read from the newest one backwards until enough entries are found. The logger keeps today's file
// open for writing, so files are opened with FileShare.ReadWrite
public sealed class LogReader(ILogPathProvider paths) : ILogReader
{
    public async Task<IReadOnlyList<LogEntry>> ReadNewestAsync(LogLevel minimumLevel, int maxEntries = ILogReader.DefaultMaxEntries)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxEntries);
        var result = new List<LogEntry>();
        if (!Directory.Exists(paths.LogDirectory))
        {
            return result;
        }

        // The date is part of the file name (stockroom-20260131.log), so name order is time order
        var files = Directory.GetFiles(paths.LogDirectory, LogFormat.FileSearchPattern)
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal);

        foreach (var file in files)
        {
            var entries = LogLineParser.Parse(await ReadLinesAsync(file));
            for (var i = entries.Count - 1; i >= 0 && result.Count < maxEntries; i--)
            {
                if (entries[i].Level >= minimumLevel)
                {
                    result.Add(entries[i]);
                }
            }

            if (result.Count >= maxEntries)
            {
                break;
            }
        }

        return result;
    }

    private static async Task<List<string>> ReadLinesAsync(string file)
    {
        var lines = new List<string>();
        await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync() is { } line)
        {
            lines.Add(line);
        }

        return lines;
    }
}
