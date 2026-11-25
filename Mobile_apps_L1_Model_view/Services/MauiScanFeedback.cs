using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

// A short buzz for a recorded scan, a long one for a refused one. A device without vibration (Windows) stays
// silent; a failure is logged and never stops the scanning
public sealed class MauiScanFeedback(IVibration vibration, ILogger<MauiScanFeedback> logger) : IScanFeedback
{
    private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(60);
    private static readonly TimeSpan Long = TimeSpan.FromMilliseconds(400);

    public void Success() => Vibrate(Short);

    public void Error() => Vibrate(Long);

    private void Vibrate(TimeSpan duration)
    {
        try
        {
            if (vibration.IsSupported)
            {
                vibration.Vibrate(duration);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Scan feedback by vibration failed");
        }
    }
}
