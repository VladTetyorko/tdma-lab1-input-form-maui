using Microsoft.Extensions.Logging;
#if ANDROID
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Camera2;
#endif

namespace Mobile_apps_L1_Model_view.Services;

// Android and iOS: the camera is checked before a preview is created, and the permission goes through MAUI Permissions
public sealed class CameraBarcodeScanner(ILogger<CameraBarcodeScanner> logger) : IBarcodeScanner
{
    public event EventHandler? Failed;

#if ANDROID
    // A device (or emulator) without a camera crashes CameraX inside the barcode library, so both the declared
    // feature and the list of cameras are asked first
    public bool IsCameraSupported
    {
        get
        {
            try
            {
                var context = Android.App.Application.Context;
                var supported = context.PackageManager?.HasSystemFeature(PackageManager.FeatureCameraAny) == true
                                && context.GetSystemService(Context.CameraService) is CameraManager cameras
                                && cameras.GetCameraIdList().Length > 0;
                if (!supported)
                {
                    logger.LogWarning("This device has no usable camera");
                }

                return supported;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "The cameras of this device could not be listed");
                return false;
            }
        }
    }
#else
    public bool IsCameraSupported => true;
#endif

    public async Task<bool> RequestCameraAccessAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        return status == PermissionStatus.Granted;
    }

    public void ReportStartFailure() => Failed?.Invoke(this, EventArgs.Empty);
}
