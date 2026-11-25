#if ANDROID || IOS
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
#endif
using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

// Toolkit toast / snackbar on phones. On Windows this unpackaged app has no notification identity, so nothing is shown:
// pages keep their own status text and buttons. A toast that cannot be shown never breaks the action that asked for it
public sealed class MauiToastService(ILogger<MauiToastService> logger) : IToastService
{
    public async Task ShowAsync(string message)
    {
#if ANDROID || IOS
        try
        {
            await Toast.Make(message, ToastDuration.Short).Show();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "A toast could not be shown");
        }
#else
        logger.LogDebug("Toasts are not shown on this platform");
        await Task.CompletedTask;
#endif
    }

    public async Task ShowAsync(string message, string actionText, Action action)
    {
#if ANDROID || IOS
        try
        {
            await Snackbar.Make(message, action, actionText, TimeSpan.FromSeconds(5)).Show();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "A snackbar could not be shown");
        }
#else
        logger.LogDebug("Snackbars are not shown on this platform");
        await Task.CompletedTask;
#endif
    }
}
