namespace Mobile_apps_L1_Model_view.Services;

// Platforms without a camera implementation: the barcode is typed in
public sealed class ManualBarcodeScanner : IBarcodeScanner
{
    public bool IsCameraSupported => false;

    public Task<bool> RequestCameraAccessAsync() => Task.FromResult(false);
}
