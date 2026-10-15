namespace Mobile_apps_L1_Model_view.Services;

public sealed class ThemeService(IPreferencesStore store, IThemeApplier applier) : IThemeService
{
    private const string Key = "theme";

    public ThemeSetting Current { get; private set; } = ThemeSetting.System;

    public void Load()
    {
        Current = Enum.TryParse<ThemeSetting>(store.Get(Key), out var saved) && Enum.IsDefined(saved)
            ? saved
            : ThemeSetting.System;
        applier.Apply(Current);
    }

    public void Set(ThemeSetting theme)
    {
        Current = theme;
        store.Set(Key, theme.ToString());
        applier.Apply(theme);
    }
}
