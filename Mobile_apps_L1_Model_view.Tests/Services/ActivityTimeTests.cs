using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ActivityTimeTests
{
    // Local times, so the test does not depend on the time zone of the machine
    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local).ToUniversalTime();

    private static readonly DateTime Now = Utc(2026, 11, 25, 18, 0);

    [Theory]
    [InlineData(2026, 11, 25, "Today 17:58")]
    [InlineData(2026, 1, 1, "1 Jan 17:58")]
    [InlineData(2025, 11, 25, "25 Nov 2025 17:58")]
    public void ShowsLocalTime_ShortForToday_WithTheYearForAnotherYear(int year, int month, int day, string expected) =>
        Assert.Equal(expected, ActivityTime.Format(Utc(year, month, day, 17, 58), Now));
}
