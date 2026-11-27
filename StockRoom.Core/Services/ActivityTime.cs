using System.Globalization;

namespace Mobile_apps_L1_Model_view.Services;

// The one way a moment of an operation is shown, in local time: "Today 17:58", "25 Nov 17:58",
// and with the year when it is not this year
public static class ActivityTime
{
    public static string Format(DateTime utc, DateTime nowUtc)
    {
        var local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
        var now = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc).ToLocalTime();
        var format = local.Date == now.Date ? "'Today' HH:mm" : local.Year == now.Year ? "d MMM HH:mm" : "d MMM yyyy HH:mm";
        return local.ToString(format, CultureInfo.InvariantCulture);
    }
}
