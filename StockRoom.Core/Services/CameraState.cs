namespace Mobile_apps_L1_Model_view.Services;

// Where a page with a camera stands
public enum CameraState
{
    // Not asked yet, or switched off
    Off,

    // The preview runs and reads codes
    Active,

    // The user refused the camera permission
    Denied,

    // No camera scanning on this device
    Unsupported
}
