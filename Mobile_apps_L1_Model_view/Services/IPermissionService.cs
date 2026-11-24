namespace Mobile_apps_L1_Model_view.Services;

// Check, explain, ask: the one way the app obtains Camera, Photos and Location. Never throws
public interface IPermissionService
{
    // Granted at once when the permission is already given. Otherwise the user first reads why the app needs it
    // (IAlertService); only if they agree does the system prompt appear. "Not now" counts as Denied
    Task<PermissionResult> EnsureAsync(DeviceFeature feature);
}
