using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Outcome of asking for the current position. Point is set only when Status is Succeeded
public sealed record LocationResult(DeviceStatus Status, GeoPoint? Point = null, Exception? Error = null)
{
    public static LocationResult Found(GeoPoint point) => new(DeviceStatus.Succeeded, point);

    public static LocationResult Of(DeviceStatus status) => new(status);

    public static LocationResult Failure(Exception error) => new(DeviceStatus.Failed, null, error);
}
