using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly TestStore _store = new();
    private readonly FakeThemeApplier _applier = new();
    private readonly ThemeService _theme;
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _theme = new ThemeService(_store.Preferences, _applier);
        _viewModel = new SettingsViewModel(_theme, _store.DeepLinks);
    }

    [Fact]
    public void ThemeOptions_ListsSystemLightAndDark()
    {
        Assert.Equal(new[] { ThemeSetting.System, ThemeSetting.Light, ThemeSetting.Dark }, _viewModel.ThemeOptions);
    }

    [Fact]
    public void SelectedTheme_StartsWithTheCurrentChoice()
    {
        _theme.Set(ThemeSetting.Dark);

        var viewModel = new SettingsViewModel(_theme, _store.DeepLinks);

        Assert.Equal(ThemeSetting.Dark, viewModel.SelectedTheme);
    }

    [Fact]
    public void SelectedTheme_ChangeSavesAndAppliesIt()
    {
        _viewModel.SelectedTheme = ThemeSetting.Light;

        Assert.Equal(ThemeSetting.Light, _theme.Current);
        Assert.Equal("Light", _store.Preferences.Values["theme"]);
        Assert.Equal(ThemeSetting.Light, _applier.Applied.Last());
    }

    [Fact]
    public async Task OpenLink_ValidLink_OpensTheProduct()
    {
        const string barcode = "4820001000016";
        await _store.AddProductAsync("Drill", barcode);
        await _store.SignInAsync(Role.Storekeeper);
        _viewModel.LinkText = DeepLinkParser.ForProduct(barcode);

        await _viewModel.OpenLinkCommand.ExecuteAsync(null);

        Assert.Equal(Routes.ProductDetail, Assert.Single(_store.Navigation.RequestedRoutes));
    }

    [Fact]
    public async Task OpenLink_NotALink_ShowsTheUnsupportedLinkAlert()
    {
        _viewModel.LinkText = "hello";

        await _viewModel.OpenLinkCommand.ExecuteAsync(null);

        Assert.Equal("Link not supported", Assert.Single(_store.Alerts.Shown).Title);
    }
}
