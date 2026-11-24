using System.Text;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class LogReaderTests : IDisposable
{
    private readonly TempLogPath _paths = new();
    private readonly LogReader _reader;

    public LogReaderTests()
    {
        Directory.CreateDirectory(_paths.LogDirectory);
        _reader = new LogReader(_paths);
    }

    public void Dispose() => _paths.Dispose();

    private static string Line(int minute, string level, string message) =>
        $"2026-01-31T10:{minute:00}:00.0000000+00:00 [{level}] A.Source {message}";

    private void WriteFile(string name, params string[] lines) =>
        File.WriteAllLines(Path.Combine(_paths.LogDirectory, name), lines);

    [Fact]
    public async Task ReadNewestAsync_NoFolder_IsEmpty()
    {
        var reader = new LogReader(new TempLogPath());

        Assert.Empty(await reader.ReadNewestAsync(LogLevel.Debug));
    }

    [Fact]
    public async Task ReadNewestAsync_ReturnsNewestFirst()
    {
        WriteFile("stockroom-20260131.log", Line(1, "INF", "first"), Line(2, "INF", "second"), Line(3, "INF", "third"));

        var entries = await _reader.ReadNewestAsync(LogLevel.Debug);

        Assert.Equal(["third", "second", "first"], entries.Select(e => e.Message));
    }

    [Fact]
    public async Task ReadNewestAsync_FiltersByMinimumLevel()
    {
        WriteFile("stockroom-20260131.log",
            Line(1, "DBG", "debug"), Line(2, "INF", "info"), Line(3, "WRN", "warning"), Line(4, "ERR", "error"));

        var warningsUp = await _reader.ReadNewestAsync(LogLevel.Warning);

        Assert.Equal(["error", "warning"], warningsUp.Select(e => e.Message));
    }

    [Fact]
    public async Task ReadNewestAsync_StopsAtTheLimit_KeepingTheNewest()
    {
        WriteFile("stockroom-20260131.log", Enumerable.Range(1, 10).Select(i => Line(i, "INF", $"entry {i}")).ToArray());

        var entries = await _reader.ReadNewestAsync(LogLevel.Debug, maxEntries: 3);

        Assert.Equal(["entry 10", "entry 9", "entry 8"], entries.Select(e => e.Message));
    }

    [Fact]
    public async Task ReadNewestAsync_ContinuesIntoOlderFilesAndReadsTheNewestFileFirst()
    {
        WriteFile("stockroom-20260130.log", Line(1, "INF", "old 1"), Line(2, "INF", "old 2"));
        WriteFile("stockroom-20260131.log", Line(1, "INF", "new 1"));

        var entries = await _reader.ReadNewestAsync(LogLevel.Debug);

        Assert.Equal(["new 1", "old 2", "old 1"], entries.Select(e => e.Message));
    }

    [Fact]
    public async Task ReadNewestAsync_IgnoresFilesThatAreNotLogs()
    {
        WriteFile("stockroom-20260131.log", Line(1, "INF", "real"));
        WriteFile("notes.txt", Line(2, "INF", "not a log"));

        var entries = await _reader.ReadNewestAsync(LogLevel.Debug);

        Assert.Equal("real", Assert.Single(entries).Message);
    }

    [Fact]
    public async Task ReadNewestAsync_KeepsTheExceptionTextOfAMultiLineEntry()
    {
        WriteFile("stockroom-20260131.log",
            Line(1, "ERR", "Push failed"), "System.InvalidOperationException: boom", "   at A.B() in B.cs:line 1");

        var entry = Assert.Single(await _reader.ReadNewestAsync(LogLevel.Error));

        Assert.StartsWith("System.InvalidOperationException: boom", entry.Exception);
        Assert.Contains("at A.B()", entry.Exception);
    }

    // The file logger keeps the current file open for writing; the viewer must still read it
    [Fact]
    public async Task ReadNewestAsync_ReadsAFileThatAWriterHasOpen()
    {
        var path = Path.Combine(_paths.LogDirectory, "stockroom-20260131.log");
        await using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        var bytes = Encoding.UTF8.GetBytes(Line(1, "INF", "written while open") + Environment.NewLine);
        await writer.WriteAsync(bytes);
        await writer.FlushAsync();

        var entries = await _reader.ReadNewestAsync(LogLevel.Debug);

        Assert.Equal("written while open", Assert.Single(entries).Message);
    }

    [Fact]
    public async Task ReadNewestAsync_ZeroLimit_IsRefused()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _reader.ReadNewestAsync(LogLevel.Debug, 0));
    }
}
