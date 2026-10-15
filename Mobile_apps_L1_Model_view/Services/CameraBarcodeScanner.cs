namespace Mobile_apps_L1_Model_view.Services;

// Android and iOS: the camera permission goes through MAUI Permissions
public sealed class CameraBarcodeScanner : IBarcodeScanner
{
    public bool IsCameraSupported => true;

    public async Task<bool> RequestCameraAccessAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        return status == PermissionStatus.Granted;
    }
}
