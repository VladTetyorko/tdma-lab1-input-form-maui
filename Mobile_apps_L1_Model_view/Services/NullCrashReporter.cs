namespace Mobile_apps_L1_Model_view.Services;

// The default when no DSN is configured
public sealed class NullCrashReporter : ICrashReporter
{
    public bool IsEnabled => false;

    public void Report(Exception exception, string? context = null)
    {
    }
}
