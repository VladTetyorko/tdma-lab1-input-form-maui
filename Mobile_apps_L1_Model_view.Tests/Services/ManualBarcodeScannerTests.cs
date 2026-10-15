using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ManualBarcodeScannerTests
{
    [Fact]
    public void IsCameraSupported_IsFalse()
    {
        IBarcodeScanner scanner = new ManualBarcodeScanner();

        Assert.False(scanner.IsCameraSupported);
    }

    [Fact]
    public async Task RequestCameraAccessAsync_IsDenied()
    {
        IBarcodeScanner scanner = new ManualBarcodeScanner();

        Assert.False(await scanner.RequestCameraAccessAsync());
    }
}
