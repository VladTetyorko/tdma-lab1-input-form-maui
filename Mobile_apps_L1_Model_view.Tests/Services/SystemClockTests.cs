using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class SystemClockTests
{
    [Fact]
    public void UtcNow_IsCurrentUtcTime()
    {
        var before = DateTime.UtcNow;
        var now = new SystemClock().UtcNow;
        var after = DateTime.UtcNow;

        Assert.Equal(DateTimeKind.Utc, now.Kind);
        Assert.InRange(now, before, after);
    }
}
