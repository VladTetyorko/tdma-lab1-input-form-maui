using Microsoft.Maui.Handlers;

namespace Mobile_apps_L1_Model_view.Views.Components;

// Takes the native frame and underline off BorderlessEntry on Android and Windows
public static class BorderlessEntryMapping
{
    public static void Apply() => EntryHandler.Mapper.AppendToMapping("Borderless", (handler, view) =>
    {
        if (view is not BorderlessEntry)
        {
            return;
        }

#if ANDROID
        handler.PlatformView.BackgroundTintList =
            Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif WINDOWS
        var none = new Microsoft.UI.Xaml.Thickness(0);
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        var box = handler.PlatformView;
        box.BorderThickness = none;
        box.Background = transparent;
        box.Resources["TextControlBorderThemeThickness"] = none;
        box.Resources["TextControlBorderThemeThicknessFocused"] = none;
        box.Resources["TextControlBackgroundFocused"] = transparent;
        box.Resources["TextControlBackgroundPointerOver"] = transparent;
#endif
    });
}
