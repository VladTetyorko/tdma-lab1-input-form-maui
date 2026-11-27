namespace Mobile_apps_L1_Model_view.Services;

// Decides the palette from the user's choice and the device theme. Pure, so it is tested without MAUI
public static class ThemePalette
{
    public static PaletteKey Resolve(ThemeSetting setting, bool systemIsDark) => setting switch
    {
        ThemeSetting.Light => PaletteKey.LightGreen,
        ThemeSetting.Dark => PaletteKey.Dark,
        _ => systemIsDark ? PaletteKey.Dark : PaletteKey.LightGreen
    };
}
