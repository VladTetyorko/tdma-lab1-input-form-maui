namespace Mobile_apps_L1_Model_view.Services;

// How a device request ended. Every value except Succeeded is explained to the user and logged (IDeviceFeedback)
public enum DeviceStatus
{
    Succeeded,

    // The user closed the camera, the gallery or a dialog
    Cancelled,

    // The user (or the system policy) refused the permission
    PermissionDenied,

    // No such hardware or no app for it (FeatureNotSupportedException)
    NotSupported,

    // The hardware exists but is switched off: GPS / location services (FeatureNotEnabledException)
    NotEnabled,

    // Anything unexpected; the exception travels with the result
    Failed
}
