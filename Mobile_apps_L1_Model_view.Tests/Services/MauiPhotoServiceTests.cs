using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

// Every way a photo request can end except a successful copy: that needs a real FileResult (a platform file), so the
// copy itself is tested in PhotoStoreTests
public class MauiPhotoServiceTests : IDisposable
{
    private const string Barcode = "4006381333931";

    private readonly FakeMediaPicker _picker = new();
    private readonly FakePermissionService _permissions = new();
    private readonly FakeAppDataPath _appData = new();
    private readonly MauiPhotoService _service;

    public MauiPhotoServiceTests()
    {
        var store = new PhotoStore(_appData, new FakeClock(), NullLogger<PhotoStore>.Instance);
        _service = new MauiPhotoService(_picker, _permissions, store);
    }

    public void Dispose() => _appData.Dispose();

    [Fact]
    public async Task Capture_AsksForTheCameraThenOpensIt()
    {
        await _service.CaptureAsync(Barcode);

        Assert.Equal([DeviceFeature.Camera], _permissions.Asked);
        Assert.Equal(1, _picker.Captures);
    }

    [Fact]
    public async Task Pick_AsksForPhotosThenOpensTheGallery()
    {
        await _service.PickAsync(Barcode);

        Assert.Equal([DeviceFeature.Photos], _permissions.Asked);
        Assert.Equal(1, _picker.Picks);
    }

    [Fact]
    public async Task Capture_WithoutACamera_IsNotSupportedAndAsksForNothing()
    {
        _picker.IsCaptureSupported = false;

        var result = await _service.CaptureAsync(Barcode);

        Assert.Equal(DeviceStatus.NotSupported, result.Status);
        Assert.False(_service.IsCaptureSupported);
        Assert.Empty(_permissions.Asked);
        Assert.Equal(0, _picker.Captures);
    }

    [Fact]
    public async Task Capture_PermissionDenied_IsPermissionDeniedAndTheCameraIsNotOpened()
    {
        _permissions.Results[DeviceFeature.Camera] = PermissionResult.Denied;

        var result = await _service.CaptureAsync(Barcode);

        Assert.Equal(DeviceStatus.PermissionDenied, result.Status);
        Assert.Equal(0, _picker.Captures);
    }

    [Fact]
    public async Task Pick_PermissionDenied_IsPermissionDenied()
    {
        _permissions.Results[DeviceFeature.Photos] = PermissionResult.Denied;

        Assert.Equal(DeviceStatus.PermissionDenied, (await _service.PickAsync(Barcode)).Status);
        Assert.Equal(0, _picker.Picks);
    }

    [Fact]
    public async Task Capture_PermissionUnavailable_IsNotSupported()
    {
        _permissions.Results[DeviceFeature.Camera] = PermissionResult.Unavailable;

        Assert.Equal(DeviceStatus.NotSupported, (await _service.CaptureAsync(Barcode)).Status);
    }

    [Fact]
    public async Task Capture_UserCancels_IsCancelledAndNothingIsStored()
    {
        _picker.Result = null;

        var result = await _service.CaptureAsync(Barcode);

        Assert.Equal(DeviceStatus.Cancelled, result.Status);
        Assert.Null(result.RelativePath);
        Assert.False(Directory.Exists(Path.Combine(_appData.AppDataDirectory, PhotoPaths.Folder)));
    }

    [Fact]
    public async Task Pick_UserCancels_IsCancelled()
    {
        Assert.Equal(DeviceStatus.Cancelled, (await _service.PickAsync(Barcode)).Status);
    }

    [Fact]
    public async Task Capture_PickerThrowsFeatureNotSupported_IsNotSupported()
    {
        _picker.Throws = new FeatureNotSupportedException();

        Assert.Equal(DeviceStatus.NotSupported, (await _service.CaptureAsync(Barcode)).Status);
    }

    [Fact]
    public async Task Capture_PickerThrowsPermissionException_IsPermissionDenied()
    {
        _picker.Throws = new PermissionException("camera");

        Assert.Equal(DeviceStatus.PermissionDenied, (await _service.CaptureAsync(Barcode)).Status);
    }

    [Fact]
    public async Task Capture_AnythingElseThrown_IsFailedWithTheException()
    {
        var error = new InvalidOperationException("camera busy");
        _picker.Throws = error;

        var result = await _service.CaptureAsync(Barcode);

        Assert.Equal(DeviceStatus.Failed, result.Status);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task Capture_TheFileCannotBeRead_IsFailed()
    {
        // A FileResult without a platform file: reading it throws, which must not escape
        _picker.Result = new FileResult(Path.Combine(_appData.AppDataDirectory, "missing.jpg"));

        var result = await _service.CaptureAsync(Barcode);

        Assert.Equal(DeviceStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void ResolvePathAndDelete_GoThroughTheStore()
    {
        Assert.Null(_service.ResolvePath("photos/missing.jpg"));
        Assert.False(_service.Delete("photos/missing.jpg"));
    }
}
