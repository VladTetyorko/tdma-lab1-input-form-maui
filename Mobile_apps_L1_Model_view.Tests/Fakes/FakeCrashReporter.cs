using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeCrashReporter : ICrashReporter
{
    public bool IsEnabled { get; set; } = true;

    public bool Throws { get; set; }

    public List<(Exception Exception, string? Context)> Reports { get; } = [];

    public void Report(Exception exception, string? context = null)
    {
        Reports.Add((exception, context));
        if (Throws)
        {
            throw new InvalidOperationException("The crash service is down.");
        }
    }
}
