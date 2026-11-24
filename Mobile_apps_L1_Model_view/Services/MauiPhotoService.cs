namespace Mobile_apps_L1_Model_view.Services;

// Photos over IMediaPicker: check support and permission, take or pick, let PhotoStore copy the file into the photos
// folder, hand back the relative path. Static MAUI APIs are not used: the picker is injected
public sealed class MauiPhotoService(IMediaPicker picker, IPermissionService permissions, PhotoStore store) : IPhotoService
{
    public bool IsCaptureSupported => picker.IsCaptureSupported;

    public Task<PhotoResult> CaptureAsync(string barcode)
    {
        if (!picker.IsCaptureSupported)
        {
            return Task.FromResult(PhotoResult.Of(DeviceStatus.NotSupported));
        }

        return AcquireAsync(DeviceFeature.Camera, barcode, () => picker.CapturePhotoAsync());
    }

    public Task<PhotoResult> PickAsync(string barcode) =>
        AcquireAsync(DeviceFeature.Photos, barcode, () => picker.PickPhotoAsync());

    public string? ResolvePath(string? relativePath) => store.ResolvePath(relativePath);

    public bool Delete(string? relativePath) => store.Delete(relativePath);

    private async Task<PhotoResult> AcquireAsync(DeviceFeature feature, string barcode, Func<Task<FileResult?>> acquire)
    {
        var permission = await permissions.EnsureAsync(feature);
        if (permission != PermissionResult.Granted)
        {
            return PhotoResult.Of(permission == PermissionResult.Denied ? DeviceStatus.PermissionDenied : DeviceStatus.NotSupported);
        }

        try
        {
            var file = await acquire();
            if (file is null)
            {
                return PhotoResult.Of(DeviceStatus.Cancelled);
            }

            await using var source = await file.OpenReadAsync();
            return PhotoResult.Saved(await store.SaveAsync(source, barcode));
        }
        catch (FeatureNotSupportedException)
        {
            return PhotoResult.Of(DeviceStatus.NotSupported);
        }
        catch (PermissionException)
        {
            return PhotoResult.Of(DeviceStatus.PermissionDenied);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return PhotoResult.Failure(ex);
        }
    }
}
