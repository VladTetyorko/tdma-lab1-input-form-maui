using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class CameraMessagesTests
{
    [Theory]
    [InlineData(CameraState.Unsupported, CameraMessages.Unsupported, "")]
    [InlineData(CameraState.Denied, CameraMessages.Denied, CameraMessages.AllowAction)]
    [InlineData(CameraState.Active, "", "")]
    [InlineData(CameraState.Off, "", "")]
    public void EveryState_HasItsMessageAndAction(CameraState state, string message, string action)
    {
        Assert.Equal(message, CameraMessages.For(state));
        Assert.Equal(action, CameraMessages.ActionFor(state));
    }
}
