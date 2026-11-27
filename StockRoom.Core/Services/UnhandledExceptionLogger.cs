using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

// The last line of defence: exceptions nobody caught are written to the log (and sent to the crash service)
// before the process goes away. Platform hooks (Android, Windows) forward to OnPlatformUnhandled.
public sealed class UnhandledExceptionLogger(
    ILogger<UnhandledExceptionLogger> logger,
    ICrashReporter? crashReporter = null) : IDisposable
{
    private readonly object _gate = new();
    private bool _attached;

    // Subscribes to the two .NET-wide events. Calling it again does nothing
    public void Attach()
    {
        lock (_gate)
        {
            if (_attached)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandled;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            _attached = true;
        }
    }

    public void Detach()
    {
        lock (_gate)
        {
            if (!_attached)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandled;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            _attached = false;
        }
    }

    public void Dispose() => Detach();

    // Any thread, usually fatal (IsTerminating)
    public void OnAppDomainUnhandled(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
                        ?? new InvalidOperationException($"Non-exception object thrown: {e.ExceptionObject}");
        logger.LogCritical(exception, "Unhandled exception (terminating: {IsTerminating})", e.IsTerminating);
        Report(exception, "AppDomain.UnhandledException");
    }

    // A faulted Task nobody awaited, found by the garbage collector. Not fatal: marked observed
    public void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        logger.LogError(e.Exception, "Unobserved task exception");
        e.SetObserved();
        Report(e.Exception, "TaskScheduler.UnobservedTaskException");
    }

    // The Android and Windows UI-framework handlers: source names the hook
    public void OnPlatformUnhandled(Exception exception, string source)
    {
        logger.LogCritical(exception, "Unhandled exception from {Source}", source);
        Report(exception, source);
    }

    private void Report(Exception exception, string context)
    {
        try
        {
            crashReporter?.Report(exception, context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The crash reporter failed");
        }
    }
}
