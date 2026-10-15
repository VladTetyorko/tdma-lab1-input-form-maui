using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view;

// stockroom://product/{barcode} opens this activity (see the IntentFilter); the link goes to IDeepLinkHandler
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter([Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = DeepLinkParser.Scheme,
    DataHost = DeepLinkParser.ProductHost)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // The app is starting: no page exists yet, so the handler keeps the link until the product list is shown
        if (savedInstanceState is null && LinkOf(Intent) is { } link)
        {
            DeepLinks?.Defer(link);
        }
    }

    // The app is already running (LaunchMode.SingleTop) and another app opened a link
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (LinkOf(intent) is { } link && DeepLinks is { } handler)
        {
            _ = handler.HandleAsync(link);
        }
    }

    private static IDeepLinkHandler? DeepLinks => IPlatformApplication.Current?.Services.GetService<IDeepLinkHandler>();

    private static string? LinkOf(Intent? intent) => intent?.Action == Intent.ActionView ? intent.DataString : null;
}
