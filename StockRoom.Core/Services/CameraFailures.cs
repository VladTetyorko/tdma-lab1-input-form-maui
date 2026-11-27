namespace Mobile_apps_L1_Model_view.Services;

// Tells the camera library's start-up failures apart from every other unhandled exception. CameraX throws them on
// the UI looper, where no try block of the app can reach them; only these may be marked as handled
public static class CameraFailures
{
    private static readonly string[] Types =
    [
        "androidx.camera.core.CameraUnavailableException",
        "androidx.camera.core.InitializationException",
        "AndroidX.Camera.Core.CameraUnavailableException",
        "AndroidX.Camera.Core.InitializationException"
    ];

    // The whole exception text, so a wrapped cause counts too
    public static bool IsStartFailure(string? exceptionText) =>
        exceptionText is not null && Types.Any(type => exceptionText.Contains(type, StringComparison.Ordinal));
}
