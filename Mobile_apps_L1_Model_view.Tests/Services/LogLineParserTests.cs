using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class LogLineParserTests
{
    private const string Header = "2026-01-31T10:15:00.1234567+02:00 [INF] Mobile_apps_L1_Model_view.Services.StockService Recorded In of 4 units";

    [Fact]
    public void TryParseHeader_ReadsEveryField()
    {
        var entry = LogLineParser.TryParseHeader(Header)!;

        Assert.Equal(new DateTimeOffset(2026, 1, 31, 10, 15, 0, TimeSpan.FromHours(2)).AddTicks(1_234_567), entry.Timestamp);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("Mobile_apps_L1_Model_view.Services.StockService", entry.Source);
        Assert.Equal("StockService", entry.ShortSource);
        Assert.Equal("Recorded In of 4 units", entry.Message);
        Assert.False(entry.HasException);
    }

    [Theory]
    [InlineData("VRB", LogLevel.Trace)]
    [InlineData("DBG", LogLevel.Debug)]
    [InlineData("INF", LogLevel.Information)]
    [InlineData("WRN", LogLevel.Warning)]
    [InlineData("ERR", LogLevel.Error)]
    [InlineData("FTL", LogLevel.Critical)]
    public void TryParseHeader_MapsTheThreeLetterLevel(string code, LogLevel level)
    {
        var entry = LogLineParser.TryParseHeader($"2026-01-31T10:15:00.0000000+00:00 [{code}] A.B message");

        Assert.Equal(level, entry!.Level);
        Assert.Equal(code, entry.LevelCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   at Some.Method() in File.cs:line 5")]
    [InlineData("System.InvalidOperationException: boom")]
    [InlineData("2026-01-31T10:15:00.0000000+00:00 [XXX] A.B message")]
    [InlineData("not-a-date [INF] A.B message")]
    public void TryParseHeader_RejectsLinesThatAreNotEntries(string line)
    {
        Assert.Null(LogLineParser.TryParseHeader(line));
    }

    [Fact]
    public void Parse_AttachesContinuationLinesToThePreviousEntry()
    {
        string[] lines =
        [
            Header,
            "2026-01-31T10:16:00.0000000+02:00 [ERR] A.B Push failed",
            "System.InvalidOperationException: boom",
            "   at A.B.Run() in B.cs:line 7",
            "   at A.B.Main() in B.cs:line 3",
            "2026-01-31T10:17:00.0000000+02:00 [WRN] A.C Offline"
        ];

        var entries = LogLineParser.Parse(lines);

        Assert.Equal(3, entries.Count);
        Assert.Null(entries[0].Exception);
        Assert.Equal("Push failed", entries[1].Message);
        Assert.Equal("System.InvalidOperationException: boom\n   at A.B.Run() in B.cs:line 7\n   at A.B.Main() in B.cs:line 3",
            entries[1].Exception);
        Assert.Null(entries[2].Exception);
    }

    [Fact]
    public void Parse_DropsLinesBeforeTheFirstEntryAndTrailingBlankLines()
    {
        var entries = LogLineParser.Parse(["   at orphan line", Header, "", ""]);

        var entry = Assert.Single(entries);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public void Parse_NoLines_IsEmpty()
    {
        Assert.Empty(LogLineParser.Parse([]));
    }
}
