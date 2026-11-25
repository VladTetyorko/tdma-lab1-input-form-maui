using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeScanFeedback : IScanFeedback
{
    public int Successes { get; private set; }

    public int Errors { get; private set; }

    public void Success() => Successes++;

    public void Error() => Errors++;
}
