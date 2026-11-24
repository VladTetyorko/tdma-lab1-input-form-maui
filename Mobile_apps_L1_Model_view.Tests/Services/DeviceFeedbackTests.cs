using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class DeviceFeedbackTests
{
    private readonly List<LogRecord> _logs = [];
    private readonly DeviceFeedback _feedback;

    public DeviceFeedbackTests()
    {
        _feedback = new DeviceFeedback(new CapturingLogger<DeviceFeedback>(_logs));
    }

    [Fact]
    public void Succeeded_HasNoMessageAndLogsNothing()
    {
        Assert.Equal(string.Empty, _feedback.Report(DeviceFeature.Camera, DeviceStatus.Succeeded));
        Assert.Empty(_logs);
    }

    [Theory]
    [InlineData(DeviceStatus.PermissionDenied)]
    [InlineData(DeviceStatus.NotSupported)]
    [InlineData(DeviceStatus.NotEnabled)]
    public void ExpectedProblems_AreExplainedAndLoggedAsAWarning(DeviceStatus status)
    {
        var message = _feedback.Report(DeviceFeature.Location, status);

        Assert.False(string.IsNullOrWhiteSpace(message));
        Assert.Equal(LogLevel.Warning, Assert.Single(_logs).Level);
    }

    [Fact]
    public void Cancel_IsExplainedAndLoggedAsInformation()
    {
        var message = _feedback.Report(DeviceFeature.Photos, DeviceStatus.Cancelled);

        Assert.Equal("No photo was picked.", message);
        Assert.Equal(LogLevel.Information, Assert.Single(_logs).Level);
    }

    [Fact]
    public void Failure_IsLoggedAsAnErrorWithTheException()
    {
        var error = new IOException("disk full");

        var message = _feedback.Report(DeviceFeature.Camera, DeviceStatus.Failed, error);

        Assert.Equal("The photo could not be taken. Try again.", message);
        var record = Assert.Single(_logs);
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Same(error, record.Exception);
    }

    [Theory]
    [InlineData(DeviceFeature.Camera)]
    [InlineData(DeviceFeature.Photos)]
    [InlineData(DeviceFeature.Location)]
    [InlineData(DeviceFeature.Maps)]
    public void EveryFeatureAndFailureStatusHasAMessage(DeviceFeature feature)
    {
        foreach (var status in Enum.GetValues<DeviceStatus>().Where(s => s != DeviceStatus.Succeeded))
        {
            Assert.False(string.IsNullOrWhiteSpace(DeviceMessages.For(feature, status)), $"{feature} {status}");
        }
    }
}
