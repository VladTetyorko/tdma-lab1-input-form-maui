namespace Mobile_apps_L1_Model_view.Services;

// Every text the user reads about a device capability, in one place (the matrix in docs/steps/07-device.md)
public static class DeviceMessages
{
    // Why the app asks, shown before the system permission prompt
    public static string Rationale(DeviceFeature feature) => feature switch
    {
        DeviceFeature.Camera => "StockRoom uses the camera to take a photo of a product. The photo stays on this device.",
        DeviceFeature.Photos => "StockRoom needs access to your photos so you can choose a picture of a product.",
        DeviceFeature.Location => "StockRoom uses your location only when you attach it to a stock operation, for example a drop-off point.",
        _ => "StockRoom needs this permission to continue."
    };

    // What the user reads when a request did not succeed (empty for Succeeded)
    public static string For(DeviceFeature feature, DeviceStatus status) => (feature, status) switch
    {
        (_, DeviceStatus.Succeeded) => string.Empty,

        (DeviceFeature.Camera, DeviceStatus.PermissionDenied) =>
            "Camera permission was denied. Allow the camera for StockRoom in the device settings, or pick a photo from the gallery.",
        (DeviceFeature.Camera, DeviceStatus.NotSupported) =>
            "This device has no camera StockRoom can use. Pick a photo from the gallery instead.",
        (DeviceFeature.Camera, DeviceStatus.Cancelled) => "No photo was taken.",
        (DeviceFeature.Camera, _) => "The photo could not be taken. Try again.",

        (DeviceFeature.Photos, DeviceStatus.PermissionDenied) =>
            "Access to your photos was denied. Allow it in the device settings, or take a photo with the camera.",
        (DeviceFeature.Photos, DeviceStatus.NotSupported) => "Picking a photo is not supported on this device.",
        (DeviceFeature.Photos, DeviceStatus.Cancelled) => "No photo was picked.",
        (DeviceFeature.Photos, _) => "The photo could not be added. Try again.",

        (DeviceFeature.Location, DeviceStatus.PermissionDenied) =>
            "Location permission was denied. Allow it in the device settings to attach your location.",
        (DeviceFeature.Location, DeviceStatus.NotSupported) => "This device cannot provide its location.",
        (DeviceFeature.Location, DeviceStatus.NotEnabled) =>
            "Location services are off. Turn on GPS or location in the device settings and try again.",
        (DeviceFeature.Location, DeviceStatus.Cancelled) => "No location was attached.",
        (DeviceFeature.Location, _) =>
            "Your location could not be determined. Check that location services are on and try again.",

        (DeviceFeature.Maps, DeviceStatus.NotSupported) => "No maps app is available on this device.",
        (DeviceFeature.Maps, _) => "The maps app could not be opened.",

        _ => "This is not available right now."
    };
}
