using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Counted Start / Stop like the real detector; Shake() plays a shake
public sealed class FakeShakeDetector : IShakeDetector
{
    public bool IsSupported { get; set; } = true;

    public int Starts { get; private set; }

    public int Stops { get; private set; }

    public bool IsRunning => Starts - Stops > 0;

    public int HandlerCount => Shaken?.GetInvocationList().Length ?? 0;

    public event EventHandler? Shaken;

    public bool Start()
    {
        if (!IsSupported)
        {
            return false;
        }

        Starts++;
        return true;
    }

    public void Stop() => Stops++;

    public void Shake() => Shaken?.Invoke(this, EventArgs.Empty);
}
