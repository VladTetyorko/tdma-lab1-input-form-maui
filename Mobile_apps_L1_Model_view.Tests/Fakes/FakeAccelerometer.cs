namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeAccelerometer : IAccelerometer
{
    public bool IsSupported { get; set; } = true;

    public bool IsMonitoring { get; private set; }

    public int Starts { get; private set; }

    public int Stops { get; private set; }

    // Thrown by Start (a sensor that fails to turn on)
    public Exception? StartThrows { get; set; }

    public int ShakeHandlerCount => ShakeDetected?.GetInvocationList().Length ?? 0;

#pragma warning disable CS0067 // part of the interface, not used by the tests
    public event EventHandler<AccelerometerChangedEventArgs>? ReadingChanged;
#pragma warning restore CS0067

    public event EventHandler? ShakeDetected;

    public void Start(SensorSpeed sensorSpeed)
    {
        if (StartThrows is not null)
        {
            throw StartThrows;
        }

        Starts++;
        IsMonitoring = true;
    }

    public void Stop()
    {
        Stops++;
        IsMonitoring = false;
    }

    public void Shake() => ShakeDetected?.Invoke(this, EventArgs.Empty);
}
