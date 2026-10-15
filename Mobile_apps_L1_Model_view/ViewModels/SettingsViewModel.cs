using CommunityToolkit.Mvvm.ComponentModel;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

public partial class SettingsViewModel(IThemeService themeService) : ObservableObject
{
    public IReadOnlyList<ThemeSetting> ThemeOptions { get; } = Enum.GetValues<ThemeSetting>();

    [ObservableProperty]
    public partial ThemeSetting SelectedTheme { get; set; } = themeService.Current;

    partial void OnSelectedThemeChanged(ThemeSetting value) => themeService.Set(value);
}
