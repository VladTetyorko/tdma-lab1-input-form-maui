namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// IMediaPicker without a camera or gallery: the next call returns the file given (null = the user cancelled)
// or throws
public sealed class FakeMediaPicker : IMediaPicker
{
    public bool IsCaptureSupported { get; set; } = true;

    public FileResult? Result { get; set; }

    public Exception? Throws { get; set; }

    public int Captures { get; private set; }

    public int Picks { get; private set; }

    public Task<FileResult?> CapturePhotoAsync(MediaPickerOptions? options = null)
    {
        Captures++;
        return Answer();
    }

    public Task<FileResult?> PickPhotoAsync(MediaPickerOptions? options = null)
    {
        Picks++;
        return Answer();
    }

    public Task<FileResult?> CaptureVideoAsync(MediaPickerOptions? options = null) => throw new NotSupportedException();

    public Task<FileResult?> PickVideoAsync(MediaPickerOptions? options = null) => throw new NotSupportedException();

    public Task<List<FileResult>> PickPhotosAsync(MediaPickerOptions? options = null) => throw new NotSupportedException();

    public Task<List<FileResult>> PickVideosAsync(MediaPickerOptions? options = null) => throw new NotSupportedException();

    private Task<FileResult?> Answer()
    {
        if (Throws is not null)
        {
            throw Throws;
        }

        return Task.FromResult(Result);
    }
}
