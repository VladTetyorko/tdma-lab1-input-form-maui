namespace Mobile_apps_L1_Model_view.Services;

// MAUI Permissions behind IPlatformPermissions. Photos: only iOS has a runtime permission for the gallery;
// the Android system photo picker and the Windows file picker need none, so they report Granted
public sealed class MauiPlatformPermissions : IPlatformPermissions
{
    public async Task<PermissionResult> CheckAsync(DeviceFeature feature) =>
        feature switch
        {
            DeviceFeature.Camera => Map(await Permissions.CheckStatusAsync<Permissions.Camera>()),
            DeviceFeature.Location => Map(await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>()),
#if IOS
            DeviceFeature.Photos => Map(await Permissions.CheckStatusAsync<Permissions.Photos>()),
#endif
            _ => PermissionResult.Granted
        };

    public async Task<PermissionResult> RequestAsync(DeviceFeature feature) =>
        feature switch
        {
            DeviceFeature.Camera => Map(await Permissions.RequestAsync<Permissions.Camera>()),
            DeviceFeature.Location => Map(await Permissions.RequestAsync<Permissions.LocationWhenInUse>()),
#if IOS
            DeviceFeature.Photos => Map(await Permissions.RequestAsync<Permissions.Photos>()),
#endif
            _ => PermissionResult.Granted
        };

    // Unknown (a platform that does not model the permission) is treated as "nothing to ask"
    private static PermissionResult Map(PermissionStatus status) => status switch
    {
        PermissionStatus.Granted or PermissionStatus.Limited or PermissionStatus.Unknown => PermissionResult.Granted,
        PermissionStatus.Denied => PermissionResult.Denied,
        _ => PermissionResult.Unavailable
    };
}
