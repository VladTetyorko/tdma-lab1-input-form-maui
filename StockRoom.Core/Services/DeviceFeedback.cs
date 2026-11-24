using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class DeviceFeedback(ILogger<DeviceFeedback> logger) : IDeviceFeedback
{
    public string Report(DeviceFeature feature, DeviceStatus status, Exception? error = null)
    {
        switch (status)
        {
            case DeviceStatus.Succeeded:
                return string.Empty;
            case DeviceStatus.Cancelled:
                logger.LogInformation("{Feature} was cancelled by the user", feature);
                break;
            case DeviceStatus.Failed:
                logger.LogError(error, "{Feature} failed unexpectedly", feature);
                break;
            default:
                logger.LogWarning("{Feature} is not available: {Status}", feature, status);
                break;
        }

        return DeviceMessages.For(feature, status);
    }
}
