namespace Mobile_apps_L1_Model_view.Services;

// Manager-only tools that prove logging and crash reporting work. Each refuses other roles itself
// (the service layer is the one that counts) and says what it did in the returned message.
public interface IDiagnosticsService
{
    // Whether the current user may use the tools
    bool IsAvailable { get; }

    // True when a crash service is configured ("Send test crash" is enabled)
    bool CanSendTestCrash { get; }

    // One log entry per level, Debug to Critical
    Result<string> WriteTestEntries();

    // A faulted Task nobody observes, then a garbage collection, so TaskScheduler.UnobservedTaskException fires
    Task<Result<string>> ThrowInBackgroundTaskAsync();

    // Reports a harmless exception to the crash service
    Result<string> SendTestCrash();
}
