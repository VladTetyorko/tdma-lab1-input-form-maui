using Microsoft.Extensions.Logging;
#if ANDROID
using Android.Content;
using Android.Views.InputMethods;
#endif

namespace Mobile_apps_L1_Model_view.Services;

// Android: closes the keyboard but leaves the focus where it is. Elsewhere there is no keyboard to close
public sealed class MauiKeyboard(ILogger<MauiKeyboard> logger) : IKeyboard
{
    public void Hide()
    {
#if ANDROID
        try
        {
            if (Platform.CurrentActivity?.CurrentFocus is { WindowToken: { } window } view
                && view.Context?.GetSystemService(Context.InputMethodService) is InputMethodManager input)
            {
                input.HideSoftInputFromWindow(window, HideSoftInputFlags.None);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "The keyboard could not be closed");
        }
#else
        logger.LogDebug("No on-screen keyboard to close");
#endif
    }
}
