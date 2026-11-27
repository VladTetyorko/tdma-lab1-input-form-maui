using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

// IAccelerometer.ShakeDetected behind IShakeDetector, with counted Start / Stop
public sealed class MauiShakeDetector(IAccelerometer accelerometer, ILogger<MauiShakeDetector> logger) : IShakeDetector
{
    private readonly object _gate = new();
    private int _starts;

    public bool IsSupported => accelerometer.IsSupported;

    public bool IsRunning => accelerometer.IsMonitoring;

    public event EventHandler? Shaken;

    public bool Start()
    {
        lock (_gate)
        {
            if (!accelerometer.IsSupported)
            {
                logger.LogWarning("Shake detection is not available: the device has no accelerometer");
                return false;
            }

            if (_starts == 0)
            {
                try
                {
                    accelerometer.ShakeDetected += OnShakeDetected;
                    if (!accelerometer.IsMonitoring)
                    {
                        accelerometer.Start(SensorSpeed.Game);
                    }
                }
                catch (Exception ex) when (ex is FeatureNotSupportedException or InvalidOperationException)
                {
                    accelerometer.ShakeDetected -= OnShakeDetected;
                    logger.LogWarning(ex, "The accelerometer could not be started, shake detection is off");
                    return false;
                }
            }

            _starts++;
            logger.LogDebug("Shake detection started ({Users} users)", _starts);
            return true;
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (_starts == 0)
            {
                return;
            }

            _starts--;
            if (_starts > 0)
            {
                return;
            }

            accelerometer.ShakeDetected -= OnShakeDetected;
            if (accelerometer.IsMonitoring)
            {
                accelerometer.Stop();
            }

            logger.LogDebug("Shake detection stopped");
        }
    }

    private void OnShakeDetected(object? sender, EventArgs e) => Shaken?.Invoke(this, EventArgs.Empty);
}
