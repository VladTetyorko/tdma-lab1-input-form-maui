namespace Mobile_apps_L1_Model_view.Services;

public sealed class MauiThemeApplier : IThemeApplier
{
    public void Apply(ThemeSetting theme)
    {
        if (Application.Current is { } app)
        {
            app.UserAppTheme = theme switch
            {
                ThemeSetting.Light => AppTheme.Light,
                ThemeSetting.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
    }
}
