using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class UnhandledExceptionLoggerTests : IDisposable
{
    private readonly CapturingLogger<UnhandledExceptionLogger> _log = new();
    private readonly FakeCrashReporter _reporter = new();
    private readonly UnhandledExceptionLogger _handler;

    public UnhandledExceptionLoggerTests()
    {
        _handler = new UnhandledExceptionLogger(_log, _reporter);
    }

    public void Dispose() => _handler.Dispose();

    [Fact]
    public void AppDomainHandler_LogsCriticalWithTheExceptionAndReportsIt()
    {
        var exception = new InvalidOperationException("boom");

        _handler.OnAppDomainUnhandled(this, new UnhandledExceptionEventArgs(exception, true));

        var record = Assert.Single(_log.Records);
        Assert.Equal(LogLevel.Critical, record.Level);
        Assert.Same(exception, record.Exception);
        var report = Assert.Single(_reporter.Reports);
        Assert.Same(exception, report.Exception);
        Assert.Equal("AppDomain.UnhandledException", report.Context);
    }

    [Fact]
    public void AppDomainHandler_WithANonExceptionObject_StillLogsCritical()
    {
        _handler.OnAppDomainUnhandled(this, new UnhandledExceptionEventArgs("just a string", false));

        var record = Assert.Single(_log.Records);
        Assert.Equal(LogLevel.Critical, record.Level);
        Assert.Contains("just a string", record.Exception!.Message);
    }

    [Fact]
    public void UnobservedTaskHandler_LogsErrorMarksItObservedAndReports()
    {
        var aggregate = new AggregateException(new InvalidOperationException("background"));
        var args = new UnobservedTaskExceptionEventArgs(aggregate);

        _handler.OnUnobservedTaskException(this, args);

        var record = Assert.Single(_log.Records);
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Same(aggregate, record.Exception);
        Assert.True(args.Observed);
        Assert.Single(_reporter.Reports);
    }

    [Fact]
    public void PlatformHandler_LogsCriticalWithTheSource()
    {
        _handler.OnPlatformUnhandled(new NotSupportedException("ui"), "Android.UnhandledExceptionRaiser");

        var record = Assert.Single(_log.Records);
        Assert.Equal(LogLevel.Critical, record.Level);
        Assert.Contains("Android.UnhandledExceptionRaiser", record.Message);
        Assert.Equal("Android.UnhandledExceptionRaiser", Assert.Single(_reporter.Reports).Context);
    }

    [Fact]
    public void AFailingCrashReporter_DoesNotStopTheLogging()
    {
        _reporter.Throws = true;

        _handler.OnAppDomainUnhandled(this, new UnhandledExceptionEventArgs(new Exception("x"), true));

        Assert.Equal([LogLevel.Critical, LogLevel.Error], _log.Records.Select(r => r.Level));
    }

    [Fact]
    public void WithoutACrashReporter_TheHandlersOnlyLog()
    {
        var handler = new UnhandledExceptionLogger(_log);

        handler.OnPlatformUnhandled(new Exception("x"), "test");

        Assert.Single(_log.Records);
    }

    [Fact]
    public async Task Attach_IsIdempotentAndCatchesARealUnobservedTask()
    {
        _handler.Attach();
        _handler.Attach();

        // The task faults and nobody awaits it: its exception reaches the handler when the task is collected
        ThrowInBackground();
        await Eventually.WaitAsync(() =>
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return _log.Records.Any(r => r.Level == LogLevel.Error);
        });

        // Two Attach calls = one subscription = one record per exception (other tests' tasks may add more)
        var errors = _log.Records.Where(r => r.Level == LogLevel.Error && r.Exception!.InnerException?.Message == "no one awaits me");
        Assert.Single(errors);
    }

    [Fact]
    public async Task Detach_StopsTheLogging()
    {
        _handler.Attach();
        _handler.Detach();

        ThrowInBackground();
        for (var i = 0; i < 5; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(10);
        }

        Assert.Empty(_log.Records.Where(r => r.Exception?.InnerException?.Message == "no one awaits me"));
    }

    // Separate method so the task is no longer referenced when the collector runs
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void ThrowInBackground() =>
        _ = Task.Run(() => throw new InvalidOperationException("no one awaits me"));
}
