namespace Mobile_apps_L1_Model_view.Services;

// Puts a theme on the running app. The only thing in the theme feature that touches Application.Current
public interface IThemeApplier
{
    void Apply(ThemeSetting theme);
}
