using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class PhotoPathsTests
{
    [Fact]
    public void Build_IsRelative_WithBarcodeAndUtcTimestamp()
    {
        var path = PhotoPaths.Build("4006381333931", new DateTime(2026, 1, 15, 9, 30, 5, DateTimeKind.Utc));

        Assert.Equal("photos/4006381333931_20260115_093005.jpg", path);
        Assert.True(PhotoPaths.IsValidRelative(path));
    }

    [Theory]
    [InlineData("photos/a.jpg", true)]
    [InlineData("photos/sub/a.jpg", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("a.jpg", false)]
    [InlineData("/photos/a.jpg", false)]
    [InlineData("C:/photos/a.jpg", false)]
    [InlineData("photos/../secret.db3", false)]
    [InlineData("photos\a.jpg", false)]
    [InlineData("other/a.jpg", false)]
    public void IsValidRelative_AcceptsOnlyPathsInsideThePhotosFolder(string? path, bool expected)
    {
        Assert.Equal(expected, PhotoPaths.IsValidRelative(path));
    }
}
