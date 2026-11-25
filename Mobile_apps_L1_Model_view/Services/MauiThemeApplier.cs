#if ANDROID || IOS
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
#endif
using Mobile_apps_L1_Model_view.Resources.Styles.Themes;

namespace Mobile_apps_L1_Model_view.Services;

// Puts a theme on the running app: sets UserAppTheme (so native parts follow) and swaps the colour dictionary
// that every style reads through DynamicResource. For System it follows the device theme while the app runs
public sealed class MauiThemeApplier : IThemeApplier
{
    private ThemeSetting _setting = ThemeSetting.System;
    private bool _subscribed;

    public void Apply(ThemeSetting theme)
    {
        _setting = theme;
        if (Application.Current is not { } app)
        {
            return;
        }

        if (!_subscribed)
        {
            app.RequestedThemeChanged += (_, _) => Refresh();
            _subscribed = true;
        }

        app.UserAppTheme = theme switch
        {
            ThemeSetting.Light => AppTheme.Light,
            ThemeSetting.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
        Refresh();
    }

    private void Refresh()
    {
        if (Application.Current is not { } app)
        {
            return;
        }

        var systemIsDark = app.PlatformAppTheme == AppTheme.Dark;
        ResourceDictionary theme = ThemePalette.Resolve(_setting, systemIsDark) == PaletteKey.Dark ? new Dark() : new LightGreen();
        ThemeDictionarySwapper.Swap(app.Resources.MergedDictionaries, theme);
        ShowStatusBar();
    }

    // The status bar has the page background, with icons that can be read on it. Also called by AppShell: when the
    // theme is first applied there is no window yet
    public static void ShowStatusBar()
    {
#if ANDROID || IOS
        if (Application.Current?.Resources.TryGetValue("Background", out var value) != true || value is not Color background)
        {
            return;
        }

        var style = background.GetLuminosity() < 0.5f ? StatusBarStyle.LightContent : StatusBarStyle.DarkContent;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                StatusBar.SetColor(background);
                StatusBar.SetStyle(style);
            }
            catch (Exception)
            {
                // No window yet, or a platform that does not allow it: the bar keeps its colour
            }
        });
#endif
    }
}
