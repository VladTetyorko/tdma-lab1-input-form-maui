namespace Mobile_apps_L1_Model_view.Services;

// The fixed log file format, in one place: MauiProgram writes it, LogLineParser reads it.
// Changing it needs a test change (see LogLineParserTests and the Serilog round-trip test).
public static class LogFormat
{
    // Timestamp:o is ISO 8601 with offset; Level:u3 is INF/WRN/ERR/...; the exception goes on the following lines
    public const string OutputTemplate =
        "{Timestamp:o} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}";

    // Serilog inserts the date before the extension: stockroom-20260101.log
    public const string FileNamePattern = "stockroom-.log";

    public const string FileSearchPattern = "stockroom-*.log";

    public const int RetainedFiles = 7;
}
