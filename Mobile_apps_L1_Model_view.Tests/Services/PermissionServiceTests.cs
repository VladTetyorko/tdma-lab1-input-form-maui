using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class PermissionServiceTests
{
    private readonly FakePlatformPermissions _platform = new();
    private readonly FakeAlertService _alerts = new();
    private readonly List<LogRecord> _logs = [];
    private readonly PermissionService _service;

    public PermissionServiceTests()
    {
        _service = new PermissionService(_platform, _alerts, new CapturingLogger<PermissionService>(_logs));
    }

    [Fact]
    public async Task AlreadyGranted_ReturnsGrantedWithoutAskingOrExplaining()
    {
        var result = await _service.EnsureAsync(DeviceFeature.Camera);

        Assert.Equal(PermissionResult.Granted, result);
        Assert.Empty(_platform.Requested);
        Assert.Empty(_alerts.Confirmations);
    }

    [Fact]
    public async Task NotGranted_ExplainsWhyThenAsksAndReturnsTheAnswer()
    {
        _platform.State[DeviceFeature.Camera] = PermissionResult.Denied;
        _platform.RequestResult = PermissionResult.Granted;

        var result = await _service.EnsureAsync(DeviceFeature.Camera);

        Assert.Equal(PermissionResult.Granted, result);
        var rationale = Assert.Single(_alerts.Confirmations);
        Assert.Equal(DeviceMessages.Rationale(DeviceFeature.Camera), rationale.Message);
        Assert.Equal([DeviceFeature.Camera], _platform.Requested);
    }

    [Fact]
    public async Task NotGranted_UserRefusesTheExplanation_IsDeniedWithoutTheSystemPrompt()
    {
        _platform.State[DeviceFeature.Location] = PermissionResult.Denied;
        _alerts.ConfirmAnswer = false;

        var result = await _service.EnsureAsync(DeviceFeature.Location);

        Assert.Equal(PermissionResult.Denied, result);
        Assert.Empty(_platform.Requested);
    }

    [Fact]
    public async Task NotGranted_SystemPromptRefused_IsDenied()
    {
        _platform.State[DeviceFeature.Location] = PermissionResult.Denied;
        _platform.RequestResult = PermissionResult.Denied;

        Assert.Equal(PermissionResult.Denied, await _service.EnsureAsync(DeviceFeature.Location));
    }

    [Fact]
    public async Task Unavailable_IsReturnedAsIsWithoutAnExplanation()
    {
        _platform.State[DeviceFeature.Camera] = PermissionResult.Unavailable;

        Assert.Equal(PermissionResult.Unavailable, await _service.EnsureAsync(DeviceFeature.Camera));
        Assert.Empty(_alerts.Confirmations);
    }

    [Fact]
    public async Task PlatformThrows_IsUnavailableAndLoggedAsAnError()
    {
        _platform.Throws = new InvalidOperationException("no activity");

        var result = await _service.EnsureAsync(DeviceFeature.Camera);

        Assert.Equal(PermissionResult.Unavailable, result);
        var record = Assert.Single(_logs, l => l.Level == LogLevel.Error);
        Assert.IsType<InvalidOperationException>(record.Exception);
    }

    [Theory]
    [InlineData(DeviceFeature.Camera)]
    [InlineData(DeviceFeature.Photos)]
    [InlineData(DeviceFeature.Location)]
    public void Rationale_ExplainsEachPermission(DeviceFeature feature)
    {
        Assert.False(string.IsNullOrWhiteSpace(DeviceMessages.Rationale(feature)));
    }
}
