namespace Mobile_apps_L1_Model_view.Services;

// Outcome of taking or picking a photo. RelativePath is set only when Status is Succeeded
public sealed record PhotoResult(DeviceStatus Status, string? RelativePath = null, Exception? Error = null)
{
    public static PhotoResult Saved(string relativePath) => new(DeviceStatus.Succeeded, relativePath);

    public static PhotoResult Of(DeviceStatus status) => new(status);

    public static PhotoResult Failure(Exception error) => new(DeviceStatus.Failed, null, error);
}
