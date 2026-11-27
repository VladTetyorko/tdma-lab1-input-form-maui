namespace Mobile_apps_L1_Model_view.Services;

// The device capabilities of Lab 7. Camera, Photos and Location are also runtime permissions (IPermissionService)
public enum DeviceFeature
{
    // Taking a photo
    Camera,

    // Picking a photo from the gallery
    Photos,

    // The current position (GPS / network)
    Location,

    // Showing a place in the maps app
    Maps
}
