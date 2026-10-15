using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

public partial class SettingsViewModel(IThemeService themeService, IDeepLinkHandler deepLinks) : ObservableObject
{
    public IReadOnlyList<ThemeSetting> ThemeOptions { get; } = Enum.GetValues<ThemeSetting>();

    [ObservableProperty]
    public partial ThemeSetting SelectedTheme { get; set; } = themeService.Current;

    // A stockroom:// link typed by hand: the way to try deep links where no other app can send one (Windows)
    [ObservableProperty]
    public partial string LinkText { get; set; } = string.Empty;

    partial void OnSelectedThemeChanged(ThemeSetting value) => themeService.Set(value);

    [RelayCommand]
    private Task OpenLinkAsync() => deepLinks.HandleAsync(LinkText);
}
