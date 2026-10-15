using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc);
}
