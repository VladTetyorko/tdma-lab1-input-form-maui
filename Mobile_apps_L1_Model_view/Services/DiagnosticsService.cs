using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class DiagnosticsService(
    ICurrentUserService currentUser,
    ICrashReporter crashReporter,
    ILogger<DiagnosticsService> logger) : IDiagnosticsService
{
    public bool IsAvailable => currentUser.CurrentUser is { } user && RolePermissions.CanUseDiagnostics(user.Role);

    public bool CanSendTestCrash => crashReporter.IsEnabled;

    public Result<string> WriteTestEntries()
    {
        if (Deny("write test log entries") is { } denied)
        {
            return denied;
        }

        logger.LogDebug("Diagnostics test entry (Debug)");
        logger.LogInformation("Diagnostics test entry (Information)");
        logger.LogWarning("Diagnostics test entry (Warning)");
        logger.LogError(new InvalidOperationException("Diagnostics test exception"), "Diagnostics test entry (Error)");
        logger.LogCritical("Diagnostics test entry (Critical)");
        return Result<string>.Success("Wrote one test entry per level (Debug, Information, Warning, Error, Critical).");
    }

    public async Task<Result<string>> ThrowInBackgroundTaskAsync()
    {
        if (Deny("throw in a background task") is { } denied)
        {
            return denied;
        }

        logger.LogInformation("Diagnostics: throwing in a background task nobody observes");
        StartFaultedTask();

        // The exception reaches TaskScheduler.UnobservedTaskException only when the task is finalized
        await Task.Delay(50);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return Result<string>.Success("A background task threw and nobody awaited it. Open the logs: an Error entry should appear.");
    }

    public Result<string> SendTestCrash()
    {
        if (Deny("send a test crash") is { } denied)
        {
            return denied;
        }

        if (!crashReporter.IsEnabled)
        {
            logger.LogWarning("Test crash not sent: no crash service is configured");
            return Result<string>.Failure("No crash service is configured. Set a Sentry DSN first (see the README).");
        }

        crashReporter.Report(new InvalidOperationException("StockRoom test crash"), "Diagnostics.SendTestCrash");
        logger.LogInformation("Diagnostics: test crash sent to the crash service");
        return Result<string>.Success("A test crash was sent to the crash service.");
    }

    // A separate method that is not inlined, so the task is unreachable once it returns
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void StartFaultedTask() =>
        _ = Task.Run(() => throw new InvalidOperationException("Diagnostics: exception in a background task"));

    // Null when allowed; otherwise the failure for the user, logged as a Warning
    private Result<string>? Deny(string action)
    {
        if (IsAvailable)
        {
            return null;
        }

        logger.LogWarning("Diagnostics denied: role {Role} cannot {Action}", currentUser.CurrentUser?.Role, action);
        return Result<string>.Failure("Only a Manager can use the diagnostics.");
    }
}
