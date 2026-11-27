using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ShakeGestureTests
{
    private readonly FakeShakeDetector _detector = new();
    private readonly FakeShakeSettings _settings = new();
    private readonly CurrentUserService _currentUser = new();
    private readonly FakeMainThread _mainThread = new();
    private readonly FakeClock _clock = new();
    private readonly List<LogRecord> _logs = [];
    private readonly ShakeGesture _gesture;
    private int _shakes;

    public ShakeGestureTests()
    {
        _currentUser.SignIn(new User { FullName = "Olena", Role = Role.Storekeeper });
        _gesture = new ShakeGesture(_detector, _settings, _currentUser, _mainThread, _clock, new CapturingLogger<ShakeGesture>(_logs));
    }

    private Task OnShake()
    {
        _shakes++;
        return Task.CompletedTask;
    }

    [Fact]
    public void Start_StartsTheSensorAndShakeRunsTheAction_OnTheMainThread()
    {
        _gesture.Start(OnShake);

        _detector.Shake();

        Assert.True(_gesture.IsActive);
        Assert.True(_detector.IsRunning);
        Assert.Equal(1, _shakes);
        Assert.Equal(1, _mainThread.Invocations);
    }

    [Fact]
    public void Stop_StopsTheSensorAndUnsubscribes()
    {
        _gesture.Start(OnShake);

        _gesture.Stop();
        _detector.Shake();

        Assert.False(_gesture.IsActive);
        Assert.False(_detector.IsRunning);
        Assert.Equal(0, _detector.HandlerCount);
        Assert.Equal(0, _shakes);
    }

    [Fact]
    public void Stop_WithoutStart_DoesNothing()
    {
        _gesture.Stop();

        Assert.Equal(0, _detector.Stops);
    }

    [Fact]
    public void StartTwice_StartsTheSensorOnce()
    {
        _gesture.Start(OnShake);
        _gesture.Start(OnShake);

        Assert.Equal(1, _detector.Starts);
        Assert.Equal(1, _detector.HandlerCount);
    }

    [Fact]
    public void RepeatedShakesInsideTheWindow_RunTheActionOnce()
    {
        _gesture.Start(OnShake);

        _detector.Shake();
        _clock.UtcNow = _clock.UtcNow.AddMilliseconds(500);
        _detector.Shake();
        _clock.UtcNow = _clock.UtcNow.AddMilliseconds(1000);
        _detector.Shake();

        Assert.Equal(1, _shakes);
    }

    [Fact]
    public void ShakeAfterTheWindow_RunsTheActionAgain()
    {
        _gesture.Start(OnShake);

        _detector.Shake();
        _clock.UtcNow = _clock.UtcNow + ShakeGesture.DebounceWindow;
        _detector.Shake();

        Assert.Equal(2, _shakes);
    }

    [Fact]
    public void NotSignedIn_DoesNotStartTheSensor()
    {
        _currentUser.SignOut();

        _gesture.Start(OnShake);

        Assert.False(_gesture.IsActive);
        Assert.Equal(0, _detector.Starts);
    }

    [Fact]
    public void SignedOutAfterStart_AShakeDoesNothing()
    {
        _gesture.Start(OnShake);
        _currentUser.SignOut();

        _detector.Shake();

        Assert.Equal(0, _shakes);
    }

    [Fact]
    public void SwitchedOffInSettings_DoesNotStartTheSensor()
    {
        _settings.IsEnabled = false;

        _gesture.Start(OnShake);

        Assert.False(_gesture.IsActive);
        Assert.Equal(0, _detector.Starts);
    }

    [Fact]
    public void DeviceWithoutAccelerometer_StaysInactive()
    {
        _detector.IsSupported = false;

        _gesture.Start(OnShake);

        Assert.False(_gesture.IsActive);
        Assert.Equal(0, _detector.HandlerCount);
    }

    [Fact]
    public void ActionThatThrows_IsLoggedAndTheGestureKeepsWorking()
    {
        _gesture.Start(() => throw new InvalidOperationException("navigation failed"));

        _detector.Shake();
        _clock.UtcNow = _clock.UtcNow + ShakeGesture.DebounceWindow;
        _detector.Shake();

        Assert.Equal(2, _logs.Count(l => l.Level == LogLevel.Error));
        Assert.True(_gesture.IsActive);
    }
}
