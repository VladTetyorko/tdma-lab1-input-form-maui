namespace Mobile_apps_L1_Model_view.Services;

// Camera and gallery behind one contract. A photo is copied into "<app data>/photos/<barcode>_<yyyyMMdd_HHmmss>.jpg"
// and the caller gets the path RELATIVE to the app data folder. Never throws: every problem is a DeviceStatus
public interface IPhotoService
{
    // False on a device without a camera (a Windows PC without a webcam): hide or disable "Take photo"
    bool IsCaptureSupported { get; }

    // Check support, ask for the camera permission, take the photo
    Task<PhotoResult> CaptureAsync(string barcode);

    // Ask for gallery access where the platform needs it, let the user choose a photo
    Task<PhotoResult> PickAsync(string barcode);

    // Full path of a stored photo for display; null when there is no photo or the file is gone
    string? ResolvePath(string? relativePath);

    // Deletes the file of a stored photo; a missing file or a bad path is ignored. Returns whether a file was removed
    bool Delete(string? relativePath);
}
