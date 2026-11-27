using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

// A short buzz and a beep for a recorded scan, a long buzz and another sound for a refused one. A device without
// vibration (Windows) stays silent; a failure is logged and never stops the scanning
public sealed class MauiScanFeedback(
    IVibration vibration,
    ITonePlayer tones,
    IScanSoundSettings sound,
    ISemanticScreenReader screenReader,
    ILogger<MauiScanFeedback> logger) : IScanFeedback
{
    private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(60);
    private static readonly TimeSpan Long = TimeSpan.FromMilliseconds(400);

    public void Success()
    {
        Vibrate(Short);
        if (sound.IsEnabled)
        {
            tones.Accepted();
        }
    }

    public void Error()
    {
        Vibrate(Long);
        if (sound.IsEnabled)
        {
            tones.Refused();
        }
    }

    public void Announce(string text)
    {
        try
        {
            screenReader.Announce(text);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Scan feedback to the screen reader failed");
        }
    }

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
