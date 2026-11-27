using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

// SentryCrashReporter lives in the app project (it needs the Sentry SDK); the DSN lookup is tested in Core (SentryDsnTests)
public class SentryCrashReporterTests
{
    [Fact]
    public void SentryCrashReporter_WithoutAnInitialisedSdk_IsDisabledAndDoesNotThrow()
    {
        var reporter = new SentryCrashReporter();

        reporter.Report(new Exception("x"), "test");

        Assert.False(reporter.IsEnabled);
    }
}
