using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

// The photo bookkeeping of a product page: staged until saved, files cleaned up, every outcome explained and logged
public class ProductPhotoEditorTests
{
    private const string Barcode = "4006381333931";

    private readonly TestStore _store = new();
    private readonly ProductPhotoEditor _editor;

    public ProductPhotoEditorTests()
    {
        _editor = _store.PhotoEditor;
    }

    private Task SignInAsync(Role role = Role.Manager) => _store.SignInAsync(role);

    [Fact]
    public async Task Take_Success_StagesTheRelativePath()
    {
        await SignInAsync();

        var message = await _editor.TakeAsync(Barcode);

        Assert.Equal(string.Empty, message);
        Assert.Equal($"photos/{Barcode}_1.jpg", _editor.Path);
        Assert.False(Path.IsPathRooted(_editor.Path));
        Assert.True(_editor.HasPhoto);
        Assert.True(_editor.IsChanged);
        Assert.Equal([Barcode], _store.Photos.CaptureBarcodes);
        Assert.NotNull(_editor.ResolvedPath);
    }

    [Fact]
    public async Task Pick_Success_StagesThePhoto()
    {
        await SignInAsync();

        await _editor.PickAsync(Barcode);

        Assert.Equal([Barcode], _store.Photos.PickBarcodes);
        Assert.True(_editor.HasPhoto);
    }

    [Theory]
    [InlineData(DeviceStatus.Cancelled, LogLevel.Information, "No photo was taken.")]
    [InlineData(DeviceStatus.PermissionDenied, LogLevel.Warning, "Camera permission was denied.")]
    [InlineData(DeviceStatus.NotSupported, LogLevel.Warning, "This device has no camera")]
    [InlineData(DeviceStatus.Failed, LogLevel.Error, "The photo could not be taken.")]
    public async Task Take_NotSucceeded_ExplainsLogsAndChangesNothing(DeviceStatus status, LogLevel level, string messageStart)
    {
        await SignInAsync();
        var error = status == DeviceStatus.Failed ? new IOException("disk full") : null;
        _store.Photos.NextResult = new PhotoResult(status, null, error);

        var message = await _editor.TakeAsync(Barcode);

        Assert.StartsWith(messageStart, message);
        Assert.Null(_editor.Path);
        Assert.False(_editor.IsChanged);
        var record = Assert.Single(_store.Logs, l => l.Category == nameof(DeviceFeedback));
        Assert.Equal(level, record.Level);
        Assert.Same(error, record.Exception);
    }

    [Theory]
    [InlineData(DeviceStatus.Cancelled, LogLevel.Information, "No photo was picked.")]
    [InlineData(DeviceStatus.PermissionDenied, LogLevel.Warning, "Access to your photos was denied.")]
    [InlineData(DeviceStatus.NotSupported, LogLevel.Warning, "Picking a photo is not supported")]
    [InlineData(DeviceStatus.Failed, LogLevel.Error, "The photo could not be added.")]
    public async Task Pick_NotSucceeded_ExplainsAndLogs(DeviceStatus status, LogLevel level, string messageStart)
    {
        await SignInAsync();
        _store.Photos.NextResult = new PhotoResult(status, null, status == DeviceStatus.Failed ? new IOException("x") : null);

        var message = await _editor.PickAsync(Barcode);

        Assert.StartsWith(messageStart, message);
        Assert.Equal(level, Assert.Single(_store.Logs, l => l.Category == nameof(DeviceFeedback)).Level);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task Take_Pick_Remove_AsNonManager_AreRefusedWithAWarning_AndTheCameraIsNotUsed(Role role)
    {
        await SignInAsync(role);

        var taken = await _editor.TakeAsync(Barcode);
        var picked = await _editor.PickAsync(Barcode);
        var removed = await _editor.RemoveAsync();

        Assert.Equal($"Your role ({role}) cannot change product photos.", taken);
        Assert.Equal(taken, picked);
        Assert.Equal($"Your role ({role}) cannot remove product photos.", removed);
        Assert.Empty(_store.Photos.CaptureBarcodes);
        Assert.Empty(_store.Photos.PickBarcodes);
        Assert.Equal(3, _store.Alerts.Shown.Count);
        Assert.Equal(3, _store.Logs.Count(l => l.Level == LogLevel.Warning));
    }

    [Fact]
    public async Task Take_NobodySignedIn_IsRefused()
    {
        var message = await _editor.TakeAsync(Barcode);

        Assert.Equal("Sign in before you change product photos.", message);
        Assert.Empty(_store.Photos.CaptureBarcodes);
    }

    [Fact]
    public async Task TakingAgain_DeletesTheUnsavedPhotoItReplaces_ButNotTheStoredOne()
    {
        await SignInAsync();
        _store.Photos.Existing.Add("photos/stored.jpg");
        _editor.Load("photos/stored.jpg");

        await _editor.TakeAsync(Barcode);
        var firstStaged = _editor.Path;
        await _editor.TakeAsync(Barcode);

        Assert.Equal([firstStaged], _store.Photos.Deleted);
        Assert.DoesNotContain("photos/stored.jpg", _store.Photos.Deleted);
        Assert.NotEqual(firstStaged, _editor.Path);
    }

    [Fact]
    public async Task TakingAPhotoWithTheSameName_DoesNotDeleteIt()
    {
        await SignInAsync();
        _store.Photos.NextResult = PhotoResult.Saved("photos/same.jpg");
        await _editor.TakeAsync(Barcode);
        _store.Photos.NextResult = PhotoResult.Saved("photos/same.jpg");

        await _editor.TakeAsync(Barcode);

        Assert.Empty(_store.Photos.Deleted);
        Assert.Equal("photos/same.jpg", _editor.Path);
    }

    [Fact]
    public async Task Commit_DeletesTheReplacedStoredFile_AndKeepsTheNewOne()
    {
        await SignInAsync();
        _store.Photos.Existing.Add("photos/old.jpg");
        _editor.Load("photos/old.jpg");
        await _editor.TakeAsync(Barcode);
        var staged = _editor.Path;

        _editor.Commit();

        Assert.Equal(["photos/old.jpg"], _store.Photos.Deleted);
        Assert.Equal(staged, _editor.Path);
        Assert.False(_editor.IsChanged);
    }

    [Fact]
    public async Task Discard_DeletesTheStagedFile_AndShowsTheStoredOneAgain()
    {
        await SignInAsync();
        _store.Photos.Existing.Add("photos/old.jpg");
        _editor.Load("photos/old.jpg");
        await _editor.TakeAsync(Barcode);
        var staged = _editor.Path;

        _editor.Discard();

        Assert.Equal([staged], _store.Photos.Deleted);
        Assert.Equal("photos/old.jpg", _editor.Path);
        Assert.False(_editor.IsChanged);
    }

    [Fact]
    public async Task Remove_OfTheStoredPhoto_KeepsItsFileUntilCommit()
    {
        await SignInAsync();
        _store.Photos.Existing.Add("photos/old.jpg");
        _editor.Load("photos/old.jpg");

        var message = await _editor.RemoveAsync();

        Assert.Equal(string.Empty, message);
        Assert.Null(_editor.Path);
        Assert.True(_editor.IsChanged);
        Assert.Empty(_store.Photos.Deleted);

        _editor.Commit();

        Assert.Equal(["photos/old.jpg"], _store.Photos.Deleted);
    }

    [Fact]
    public async Task Remove_WithoutAPhoto_IsAHarmlessNoOp()
    {
        await SignInAsync();

        Assert.Equal(string.Empty, await _editor.RemoveAsync());
        Assert.False(_editor.IsChanged);
    }

    [Fact]
    public void Load_ShowsTheStoredPhotoAsUnchanged_AndRestoreStagesADraftPhoto()
    {
        _editor.Load("photos/a.jpg");
        Assert.False(_editor.IsChanged);

        _editor.Restore("photos/b.jpg");

        Assert.True(_editor.IsChanged);
        Assert.Equal("photos/b.jpg", _editor.Path);
    }

    [Fact]
    public void IsCaptureSupported_FollowsTheDevice()
    {
        _store.Photos.IsCaptureSupported = false;

        Assert.False(_editor.IsCaptureSupported);
    }

    [Fact]
    public async Task ResolvedPath_IsNullWhenTheFileIsGone()
    {
        await SignInAsync();
        await _editor.TakeAsync(Barcode);
        _store.Photos.Existing.Clear();

        _editor.Restore(_editor.Path);

        Assert.Null(_editor.ResolvedPath);
        Assert.True(_editor.HasPhoto);
    }
}
