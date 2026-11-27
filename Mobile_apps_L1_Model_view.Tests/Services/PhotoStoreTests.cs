using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class PhotoStoreTests : IDisposable
{
    private const string Barcode = "4006381333931";

    private readonly FakeAppDataPath _appData = new();
    private readonly FakeClock _clock = new();
    private readonly PhotoStore _store;

    public PhotoStoreTests()
    {
        _store = new PhotoStore(_appData, _clock, NullLogger<PhotoStore>.Instance);
    }

    public void Dispose() => _appData.Dispose();

    private static MemoryStream Bytes(params byte[] bytes) => new(bytes);

    [Fact]
    public async Task Save_CopiesTheBytesIntoThePhotosFolder_AndReturnsTheRelativePath()
    {
        var relative = await _store.SaveAsync(Bytes(1, 2, 3), Barcode);

        Assert.Equal("photos/4006381333931_20260115_093000.jpg", relative);
        Assert.False(Path.IsPathRooted(relative));
        Assert.Equal([1, 2, 3], File.ReadAllBytes(Path.Combine(_appData.AppDataDirectory, relative)));
    }

    [Fact]
    public async Task Save_NameFollowsTheClock()
    {
        _clock.UtcNow = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc);

        Assert.Equal("photos/4006381333931_20260304_050607.jpg", await _store.SaveAsync(Bytes(1), Barcode));
    }

    [Fact]
    public async Task Save_UnsafeBarcodeCharacters_CannotEscapeTheFolder()
    {
        var relative = await _store.SaveAsync(Bytes(1), "../..\\evil");

        Assert.True(PhotoPaths.IsValidRelative(relative));
        Assert.StartsWith("photos/", relative);
        Assert.DoesNotContain("..", relative);
    }

    [Fact]
    public async Task ResolvePath_ExistingPhoto_IsTheFullPath_AndMissingOrBadIsNull()
    {
        var relative = await _store.SaveAsync(Bytes(1), Barcode);

        Assert.Equal(Path.Combine(_appData.AppDataDirectory, relative), _store.ResolvePath(relative));
        Assert.Null(_store.ResolvePath(null));
        Assert.Null(_store.ResolvePath("photos/missing.jpg"));
        Assert.Null(_store.ResolvePath("../outside.jpg"));
    }

    [Fact]
    public async Task Delete_RemovesTheFile_AndIgnoresMissingFilesAndBadPaths()
    {
        var relative = await _store.SaveAsync(Bytes(1), Barcode);
        var outside = Path.Combine(_appData.AppDataDirectory, "keep.txt");
        File.WriteAllText(outside, "keep");

        Assert.True(_store.Delete(relative));
        Assert.Null(_store.ResolvePath(relative));
        Assert.False(_store.Delete(relative));
        Assert.False(_store.Delete(null));
        Assert.False(_store.Delete("../keep.txt"));
        Assert.True(File.Exists(outside));
    }
}
