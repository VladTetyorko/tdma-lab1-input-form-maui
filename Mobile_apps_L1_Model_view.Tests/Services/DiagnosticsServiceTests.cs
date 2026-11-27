using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class DiagnosticsServiceTests
{
    private readonly TestStore _store = new();

    [Fact]
    public async Task WriteTestEntries_WritesOneEntryPerLevel()
    {
        await _store.SignInAsync(Role.Manager);

        var result = _store.Diagnostics.WriteTestEntries();

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical],
            _store.Logs.Select(r => r.Level));
        Assert.IsType<InvalidOperationException>(_store.Logs.Single(r => r.Level == LogLevel.Error).Exception);
    }

    [Fact]
    public async Task ThrowInBackgroundTask_RaisesTheUnobservedTaskHandler()
    {
        await _store.SignInAsync(Role.Manager);
        var logger = new CapturingLogger<UnhandledExceptionLogger>();
        using var handler = new UnhandledExceptionLogger(logger);
        handler.Attach();

        var result = await _store.Diagnostics.ThrowInBackgroundTaskAsync();

        Assert.True(result.IsSuccess);
        // The service asks for a collection itself; on a loaded machine the finalizer thread may need a moment
        await Eventually.WaitAsync(() =>
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return logger.Records.Any(r => r.Exception?.InnerException?.Message == "Diagnostics: exception in a background task");
        });
        Assert.Contains(logger.Records, r => r.Level == LogLevel.Error);
    }

    [Fact]
    public async Task SendTestCrash_WithACrashService_ReportsAnException()
    {
        await _store.SignInAsync(Role.Manager);

        var result = _store.Diagnostics.SendTestCrash();

        Assert.True(result.IsSuccess);
        var report = Assert.Single(_store.CrashReporter.Reports);
        Assert.Equal("Diagnostics.SendTestCrash", report.Context);
    }

    [Fact]
    public async Task SendTestCrash_WithoutACrashService_FailsWithAMessage()
    {
        await _store.SignInAsync(Role.Manager);
        _store.CrashReporter.IsEnabled = false;

        var result = _store.Diagnostics.SendTestCrash();

        Assert.False(result.IsSuccess);
        Assert.Empty(_store.CrashReporter.Reports);
        Assert.False(_store.Diagnostics.CanSendTestCrash);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task EveryTool_RefusesANonManager_WithAWarning(Role role)
    {
        await _store.SignInAsync(role);
        var diagnostics = _store.Diagnostics;

        Assert.False(diagnostics.IsAvailable);
        Assert.False(diagnostics.WriteTestEntries().IsSuccess);
        Assert.False((await diagnostics.ThrowInBackgroundTaskAsync()).IsSuccess);
        Assert.False(diagnostics.SendTestCrash().IsSuccess);

        Assert.Empty(_store.CrashReporter.Reports);
        Assert.Equal(3, _store.Logs.Count);
        Assert.All(_store.Logs, r => Assert.Equal(LogLevel.Warning, r.Level));
    }

    [Fact]
    public void NobodySignedIn_IsNotAvailable()
    {
        Assert.False(_store.Diagnostics.IsAvailable);
    }
}
