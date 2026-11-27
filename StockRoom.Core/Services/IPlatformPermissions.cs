namespace Mobile_apps_L1_Model_view.Services;

// The platform half of runtime permissions (MAUI Permissions on a device): no dialogs, no explanation text.
// IPermissionService adds the rationale and is the one the rest of the app uses
public interface IPlatformPermissions
{
    // Never shows anything; Denied also means "not asked yet"
    Task<PermissionResult> CheckAsync(DeviceFeature feature);

    // Shows the system permission prompt (or answers at once when the user already refused for good)
    Task<PermissionResult> RequestAsync(DeviceFeature feature);
}
