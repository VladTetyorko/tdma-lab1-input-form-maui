using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class CameraFailuresTests
{
    [Theory]
    [InlineData("java.lang.RuntimeException: x ---> androidx.camera.core.InitializationException: y", true)]
    [InlineData("androidx.camera.core.CameraUnavailableException: Device reporting less cameras than anticipated", true)]
    [InlineData("java.lang.IllegalStateException: something else", false)]
    [InlineData(null, false)]
    public void OnlyCameraStartFailuresAreRecognised(string? text, bool expected) =>
        Assert.Equal(expected, CameraFailures.IsStartFailure(text));
}
