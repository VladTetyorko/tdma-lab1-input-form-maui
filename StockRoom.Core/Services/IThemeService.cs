namespace Mobile_apps_L1_Model_view.Services;

// The user's theme choice: saved in preferences and applied to every page
// (styles use AppThemeBinding, so changing the theme restyles the whole app)
public interface IThemeService
{
    ThemeSetting Current { get; }

    // Reads the saved choice (System when none or unreadable) and applies it; call at start
    void Load();

    // Saves and applies
    void Set(ThemeSetting theme);
}
