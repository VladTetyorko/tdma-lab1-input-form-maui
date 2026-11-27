using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class ShakeGesture(
    IShakeDetector detector,
    IShakeSettings settings,
    ICurrentUserService currentUser,
    IMainThread mainThread,
    IClock clock,
    ILogger<ShakeGesture> logger) : IShakeGesture
{
    // A shake is physical and noisy: several events arrive for one movement, and one more movement can follow at once.
    // Only the first inside this window counts, so navigation cannot stack up
    public static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(2);

    private readonly object _gate = new();
    private Func<Task>? _onShake;
    private DateTime _lastHandled = DateTime.MinValue;

    public bool IsActive => _onShake is not null;

    public void Start(Func<Task> onShake)
    {
        if (IsActive)
        {
            return;
        }

        if (!settings.IsEnabled)
        {
            logger.LogDebug("Shake gesture not started: switched off in Settings");
            return;
        }

        if (currentUser.CurrentUser is null)
        {
            logger.LogDebug("Shake gesture not started: nobody is signed in");
            return;
        }

        if (!detector.Start())
        {
            return;
        }

        _onShake = onShake;
        detector.Shaken += OnShaken;
    }

    public void Stop()
    {
        if (!IsActive)
        {
            return;
        }

        detector.Shaken -= OnShaken;
        _onShake = null;
        detector.Stop();
    }

    // Runs on a sensor thread
    private void OnShaken(object? sender, EventArgs e)
    {
        var now = clock.UtcNow;
        lock (_gate)
        {
            if (now - _lastHandled < DebounceWindow)
            {
                return;
            }

            _lastHandled = now;
        }

        mainThread.BeginInvoke(() => _ = RunAsync());
    }

    private async Task RunAsync()
    {
        if (_onShake is not { } handler || currentUser.CurrentUser is null)
        {
            return;
        }

        try
        {
            logger.LogInformation("Shake detected");
            await handler();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The shake action failed");
        }
    }
}
