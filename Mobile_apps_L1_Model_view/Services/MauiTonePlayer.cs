using Microsoft.Extensions.Logging;
#if ANDROID
using Android.Media;
#endif

namespace Mobile_apps_L1_Model_view.Services;

// Android: the system tone generator. Other platforms have no scanner sound here
public sealed class MauiTonePlayer(ILogger<MauiTonePlayer> logger) : ITonePlayer
{
#if ANDROID
    private const int Volume = 80;
    private const int Milliseconds = 200;

    private ToneGenerator? _generator;

    public void Accepted() => Play(Tone.PropBeep);

    public void Refused() => Play(Tone.PropNack);

    private void Play(Tone tone)
    {
        try
        {
            _generator ??= new ToneGenerator(Android.Media.Stream.Notification, Volume);
            _generator.StartTone(tone, Milliseconds);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "The scan sound could not be played");
        }
    }
#else
    public void Accepted() => logger.LogDebug("No scan sound on this platform");

    public void Refused() => logger.LogDebug("No scan sound on this platform");
#endif
}
