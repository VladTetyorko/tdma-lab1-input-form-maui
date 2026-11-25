namespace Mobile_apps_L1_Model_view.Services;

// What the scanner page's ViewModel needs to know about the camera. The camera picture itself is a
// control (Views/CameraScannerView) that reports what it reads to the ViewModel through a command.
public interface IBarcodeScanner
{
    // False where there is no camera scanning (Windows, Mac Catalyst) or the device has no usable camera:
    // the page offers manual entry only. Asked before a camera preview is ever created
    bool IsCameraSupported { get; }

    // The camera of a running preview could not start (held by another app, hardware trouble)
    event EventHandler? Failed;

    // Called by the platform when the camera library failed; raises Failed
    void ReportStartFailure();

    // Asks the user for camera permission if needed. True when the camera may be used
    Task<bool> RequestCameraAccessAsync();
}
