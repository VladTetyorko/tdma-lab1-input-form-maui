using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class MauiShakeDetectorTests
{
    private readonly FakeAccelerometer _accelerometer = new();
    private readonly List<LogRecord> _logs = [];
    private readonly MauiShakeDetector _detector;

    public MauiShakeDetectorTests()
    {
        _detector = new MauiShakeDetector(_accelerometer, new CapturingLogger<MauiShakeDetector>(_logs));
    }

    [Fact]
    public void Start_TurnsTheSensorOnAndSubscribes()
    {
        Assert.True(_detector.Start());

        Assert.True(_accelerometer.IsMonitoring);
        Assert.True(_detector.IsRunning);
        Assert.Equal(1, _accelerometer.ShakeHandlerCount);
    }

    [Fact]
    public void Shake_RaisesShaken()
    {
        var raised = 0;
        _detector.Shaken += (_, _) => raised++;
        _detector.Start();

        _accelerometer.Shake();

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Stop_TurnsTheSensorOffAndUnsubscribes()
    {
        _detector.Start();

        _detector.Stop();

        Assert.False(_accelerometer.IsMonitoring);
        Assert.Equal(0, _accelerometer.ShakeHandlerCount);
        Assert.Equal(1, _accelerometer.Stops);
    }

    [Fact]
    public void Stop_WithoutStart_DoesNothing()
    {
        _detector.Stop();

        Assert.Equal(0, _accelerometer.Stops);
    }

    [Fact]
    public void TwoStarts_NeedTwoStops_AndTheSensorIsStartedOnce()
    {
        _detector.Start();
        _detector.Start();
        Assert.Equal(1, _accelerometer.Starts);

        _detector.Stop();
        Assert.True(_accelerometer.IsMonitoring);

        _detector.Stop();
        Assert.False(_accelerometer.IsMonitoring);
        Assert.Equal(0, _accelerometer.ShakeHandlerCount);
    }

    [Fact]
    public void Start_SensorAlreadyMonitoring_SubscribesWithoutStartingItAgain()
    {
        _accelerometer.Start(SensorSpeed.Game);

        _detector.Start();

        Assert.Equal(1, _accelerometer.Starts);
        Assert.Equal(1, _accelerometer.ShakeHandlerCount);
    }

    [Fact]
    public void Start_WithoutAccelerometer_ReturnsFalseAndWarns()
    {
        _accelerometer.IsSupported = false;

        Assert.False(_detector.Start());

        Assert.False(_accelerometer.IsMonitoring);
        Assert.Equal(LogLevel.Warning, Assert.Single(_logs, l => l.Level == LogLevel.Warning).Level);
    }

    [Fact]
    public void Start_SensorFailsToStart_ReturnsFalseAndLeavesNothingSubscribed()
    {
        _accelerometer.StartThrows = new FeatureNotSupportedException();

        Assert.False(_detector.Start());

        Assert.Equal(0, _accelerometer.ShakeHandlerCount);
        Assert.Contains(_logs, l => l.Level == LogLevel.Warning);
    }
}
