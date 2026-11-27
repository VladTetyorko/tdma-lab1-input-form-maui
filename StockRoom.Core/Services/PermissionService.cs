using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class PermissionService(
    IPlatformPermissions platform,
    IAlertService alerts,
    ILogger<PermissionService> logger) : IPermissionService
{
    public async Task<PermissionResult> EnsureAsync(DeviceFeature feature)
    {
        try
        {
            var current = await platform.CheckAsync(feature);
            if (current != PermissionResult.Denied)
            {
                return current;
            }

            // Not granted yet: say why before the system asks, so the prompt is not a surprise
            if (!await alerts.ConfirmAsync("Permission needed", DeviceMessages.Rationale(feature), "Continue", "Not now"))
            {
                logger.LogInformation("The user declined to be asked for {Feature} permission", feature);
                return PermissionResult.Denied;
            }

            var result = await platform.RequestAsync(feature);
            logger.LogInformation("Permission {Feature} requested: {Result}", feature, result);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A platform that cannot answer is "not available", not a crash
            logger.LogError(ex, "Permission {Feature} could not be checked", feature);
            return PermissionResult.Unavailable;
        }
    }
}
