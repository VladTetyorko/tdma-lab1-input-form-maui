using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// A scanner whose camera support and permission answer are set by the test
public sealed class FakeBarcodeScanner : IBarcodeScanner
{
    public bool IsCameraSupported { get; set; } = true;

    // What the user answers when asked for camera permission
    public bool CameraAccessGranted { get; set; } = true;

    public int AccessRequests { get; private set; }

    public Task<bool> RequestCameraAccessAsync()
    {
        AccessRequests++;
        return Task.FromResult(CameraAccessGranted);
    }
}
