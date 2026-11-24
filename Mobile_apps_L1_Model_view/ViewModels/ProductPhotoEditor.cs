using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// The photo of the product a page is editing, shared by the form and the detail page (DRY): take, pick, remove, and the
// bookkeeping of files. The photo is STAGED: the page keeps the new relative path until its Save succeeds (Commit,
// which deletes the replaced file) or the user leaves without saving (Discard, which deletes the new file).
// Every command returns the text for the user's status line; empty means it worked
public sealed partial class ProductPhotoEditor(
    IPhotoService photos,
    IDeviceFeedback feedback,
    IPermissionGuard guard,
    ILogger<ProductPhotoEditor> logger) : ObservableObject
{
    // The path saved with the product (what the database has)
    private string? _stored;

    // Relative to the app data folder; null = no photo
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    [NotifyPropertyChangedFor(nameof(ResolvedPath))]
    [NotifyPropertyChangedFor(nameof(IsChanged))]
    public partial string? Path { get; set; }

    public bool HasPhoto => Path is not null;

    // The full path the Image control shows; null when there is no photo or its file is gone
    public string? ResolvedPath => photos.ResolvePath(Path);

    // Take photo is hidden on a device without a camera (a Windows PC); Pick still works
    public bool IsCaptureSupported => photos.IsCaptureSupported;

    // The page holds a photo that is not saved with the product yet
    public bool IsChanged => Path != _stored;

    // The product as loaded or saved: its photo becomes both the stored and the shown one
    public void Load(string? storedPath)
    {
        _stored = storedPath;
        Path = storedPath;
        OnPropertyChanged(nameof(IsChanged));
    }

    // An unsaved photo from a draft: shown and staged, the stored one stays what it was
    public void Restore(string? stagedPath) => Path = stagedPath;

    public Task<string> TakeAsync(string barcode) =>
        AcquireAsync(DeviceFeature.Camera, barcode, photos.CaptureAsync);

    public Task<string> PickAsync(string barcode) =>
        AcquireAsync(DeviceFeature.Photos, barcode, photos.PickAsync);

    public async Task<string> RemoveAsync()
    {
        if (await guard.CheckAsync(RolePermissions.CanManagePhotos, "remove product photos") is { } refusal)
        {
            return refusal;
        }

        if (Path is not null)
        {
            DeleteIfStaged(Path);
            Path = null;
            logger.LogInformation("Product photo removed from the page");
        }

        return string.Empty;
    }

    // The page saved the product: the replaced file is not needed any more
    public void Commit()
    {
        if (_stored is not null && _stored != Path)
        {
            photos.Delete(_stored);
        }

        _stored = Path;
        OnPropertyChanged(nameof(IsChanged));
    }

    // The page was left without saving: a photo taken meanwhile is dropped
    public void Discard()
    {
        if (Path is not null)
        {
            DeleteIfStaged(Path);
        }

        Path = _stored;
    }

    private async Task<string> AcquireAsync(DeviceFeature feature, string barcode, Func<string, Task<PhotoResult>> acquire)
    {
        if (await guard.CheckAsync(RolePermissions.CanManagePhotos, "change product photos") is { } refusal)
        {
            return refusal;
        }

        var result = await acquire(barcode);
        if (result.Status != DeviceStatus.Succeeded)
        {
            return feedback.Report(feature, result.Status, result.Error);
        }

        var staged = result.RelativePath!;
        if (Path != staged)
        {
            // A photo taken earlier on this page and not saved is replaced, so its file goes
            if (Path is not null)
            {
                DeleteIfStaged(Path);
            }

            Path = staged;
        }

        logger.LogInformation("Photo {Source} added to the product page", feature == DeviceFeature.Camera ? "camera" : "gallery");
        return string.Empty;
    }

    // A file the database does not know yet is ours to delete; the stored one only goes on Commit
    private void DeleteIfStaged(string path)
    {
        if (path != _stored)
        {
            photos.Delete(path);
        }
    }
}
