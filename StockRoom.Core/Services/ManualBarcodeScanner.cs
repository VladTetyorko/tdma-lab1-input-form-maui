namespace Mobile_apps_L1_Model_view.Services;

// Platforms without a camera implementation: the barcode is typed in
public sealed class ManualBarcodeScanner : IBarcodeScanner
{
    public bool IsCameraSupported => false;

    // Never raised: there is no camera to fail
    public event EventHandler? Failed
    {
        add { }
        remove { }
    }

    public void ReportStartFailure()
    {
    }

    public Task<bool> RequestCameraAccessAsync() => Task.FromResult(false);
}
