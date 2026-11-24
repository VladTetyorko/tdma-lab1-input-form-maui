using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Takes and picks photos without a camera: each call answers with the next queued result (or a new file path),
// and Delete remembers which files were removed
public sealed class FakePhotoService : IPhotoService
{
    private int _counter;

    public bool IsCaptureSupported { get; set; } = true;

    // The answer of the next CaptureAsync / PickAsync; null = a fresh "photos/<barcode>_<n>.jpg"
    public PhotoResult? NextResult { get; set; }

    public List<string> CaptureBarcodes { get; } = [];

    public List<string> PickBarcodes { get; } = [];

    public List<string> Deleted { get; } = [];

    // Paths that exist, as far as ResolvePath is concerned
    public HashSet<string> Existing { get; } = [];

    public Task<PhotoResult> CaptureAsync(string barcode)
    {
        CaptureBarcodes.Add(barcode);
        return Task.FromResult(Next(barcode));
    }

    public Task<PhotoResult> PickAsync(string barcode)
    {
        PickBarcodes.Add(barcode);
        return Task.FromResult(Next(barcode));
    }

    public string? ResolvePath(string? relativePath) =>
        relativePath is not null && Existing.Contains(relativePath) ? "C:/AppData/" + relativePath : null;

    public bool Delete(string? relativePath)
    {
        if (relativePath is null)
        {
            return false;
        }

        Deleted.Add(relativePath);
        return Existing.Remove(relativePath);
    }

    private PhotoResult Next(string barcode)
    {
        var result = NextResult;
        NextResult = null;
        if (result is not null)
        {
            if (result.RelativePath is not null)
            {
                Existing.Add(result.RelativePath);
            }

            return result;
        }

        var path = $"photos/{barcode}_{++_counter}.jpg";
        Existing.Add(path);
        return PhotoResult.Saved(path);
    }
}
