using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

// Reads the file format written by LogFormat.OutputTemplate:
//   2026-01-31T10:15:00.0000000+02:00 [INF] Namespace.Class Message text
// Lines that do not start like that (stack traces, multi-line messages) belong to the previous entry.
public static partial class LogLineParser
{
    private static readonly (string Code, LogLevel Level)[] Levels =
    [
        ("VRB", LogLevel.Trace), ("DBG", LogLevel.Debug), ("INF", LogLevel.Information),
        ("WRN", LogLevel.Warning), ("ERR", LogLevel.Error), ("FTL", LogLevel.Critical)
    ];

    [GeneratedRegex(@"^(?<time>\d{4}-\d{2}-\d{2}T\S+) \[(?<level>[A-Z]{3})\] (?<source>\S*) (?<message>.*)$")]
    private static partial Regex HeaderPattern();

    public static string CodeOf(LogLevel level) =>
        Levels.FirstOrDefault(l => l.Level == level).Code ?? "INF";

    // Oldest first, like the file. Continuation lines before the first header are dropped
    public static IReadOnlyList<LogEntry> Parse(IEnumerable<string> lines)
    {
        var entries = new List<LogEntry>();
        LogEntry? current = null;
        var continuation = new StringBuilder();

        void Flush()
        {
            if (current is not null)
            {
                var text = continuation.ToString().TrimEnd();
                entries.Add(text.Length == 0 ? current : current with { Exception = text });
            }

            continuation.Clear();
        }

        foreach (var line in lines)
        {
            if (TryParseHeader(line) is { } header)
            {
                Flush();
                current = header;
            }
            else if (current is not null)
            {
                if (continuation.Length > 0)
                {
                    continuation.Append('\n');
                }

                continuation.Append(line);
            }
        }

        Flush();
        return entries;
    }

    // Null when the line is not the first line of an entry
    public static LogEntry? TryParseHeader(string line)
    {
        var match = HeaderPattern().Match(line);
        if (!match.Success
            || !DateTimeOffset.TryParse(match.Groups["time"].Value, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var time))
        {
            return null;
        }

        var code = match.Groups["level"].Value;
        var level = Levels.FirstOrDefault(l => l.Code == code);
        return level.Code is null
            ? null
            : new LogEntry(time, level.Level, match.Groups["source"].Value, match.Groups["message"].Value);
    }
}
