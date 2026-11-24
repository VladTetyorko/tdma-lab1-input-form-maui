namespace Mobile_apps_L1_Model_view.Services;

// Sends a crash or a handled error to an external crash service (Sentry). Reports carry the exception and a
// short context only, never personal data. Without a configured service the null reporter is used.
public interface ICrashReporter
{
    // False when no service is configured: Report does nothing and the UI hides "Send test crash"
    bool IsEnabled { get; }

    // Never throws: reporting must not make a failure worse
    void Report(Exception exception, string? context = null);
}
