namespace Mobile_apps_L1_Model_view.Services;

// A camera reports the same code many times a second. The same code is ignored until it has not been seen for
// the window; another code always passes. Typed and keyboard-scanner input does not go through here
public sealed class ScanDebouncer(IClock clock)
{
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(1.2);

    private string? _last;
    private DateTime _seenAt;

    public bool Accept(string barcode)
    {
        var now = clock.UtcNow;
        var repeated = barcode == _last && now - _seenAt < Window;
        _last = barcode;
        _seenAt = now;
        return !repeated;
    }
}
