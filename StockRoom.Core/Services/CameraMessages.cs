namespace Mobile_apps_L1_Model_view.Services;

// What the banner above the typing field says: the reason and the way out. Empty when there is nothing to explain
public static class CameraMessages
{
    public const string Unsupported = "No camera on this device. Type a name or barcode, or use a USB scanner.";
    public const string Denied = "Camera access is off. Allow the camera, or type a name or barcode.";
    public const string AllowAction = "Allow camera";

    public static string For(CameraState state) => state switch
    {
        CameraState.Unsupported => Unsupported,
        CameraState.Denied => Denied,
        _ => string.Empty
    };

    // Asking again makes sense only after a refusal
    public static string ActionFor(CameraState state) => state == CameraState.Denied ? AllowAction : string.Empty;
}
