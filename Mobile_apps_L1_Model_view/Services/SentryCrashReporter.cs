using Sentry;

namespace Mobile_apps_L1_Model_view.Services;

// Sentry behind ICrashReporter. The SDK itself is initialised in MauiProgram (UseSentry) only when a DSN exists.
// Only the exception and a context tag are sent: no user, no device owner, no personal data.
public sealed class SentryCrashReporter : ICrashReporter
{
    public bool IsEnabled => SentrySdk.IsEnabled;

    public void Report(Exception exception, string? context = null)
    {
        try
        {
            if (!SentrySdk.IsEnabled)
            {
                return;
            }

            SentrySdk.CaptureException(exception, scope =>
            {
                if (context is not null)
                {
                    scope.SetTag("context", context);
                }
            });
        }
        catch
        {
            // Reporting must never make a failure worse
        }
    }
}
