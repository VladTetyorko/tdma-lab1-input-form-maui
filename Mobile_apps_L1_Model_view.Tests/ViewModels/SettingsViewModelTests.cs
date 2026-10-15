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
    private readonly FakeSyncService _sync = new();
    private readonly OfflineSimulator _offline = new();
    private readonly FakeDatabaseExporter _exporter = new();
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _theme = new ThemeService(_store.Preferences, _applier);
        _viewModel = new SettingsViewModel(_theme, _store.DeepLinks, _sync, _offline, _exporter);
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

        var viewModel = new SettingsViewModel(_theme, _store.DeepLinks, _sync, _offline, _exporter);

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

    [Fact]
    public async Task OnAppearing_ShowsTheNumberOfPendingChanges()
    {
        _sync.Pending = 4;

        ((IPageLifecycleAware)_viewModel).OnAppearing();

        await Eventually.WaitAsync(() => _viewModel.PendingCount == 4);
        ((IPageLifecycleAware)_viewModel).OnDisappearing();
    }

    [Fact]
    public async Task SyncNow_ShowsWhatWasSent()
    {
        _sync.Result = new SyncResult(SyncStatus.Synced, 3, 0);

        await _viewModel.SyncNowCommand.ExecuteAsync(null);

        Assert.Equal(1, _sync.SyncNowCalls);
        Assert.Equal(0, _viewModel.PendingCount);
        Assert.Equal("Synced: 3 sent, 0 waiting.", _viewModel.SyncText);
    }

    [Fact]
    public async Task SyncNow_Failed_ShowsTheReasonAndWhatStaysQueued()
    {
        _sync.Result = new SyncResult(SyncStatus.Failed, 1, 2, "server down");

        await _viewModel.SyncNowCommand.ExecuteAsync(null);

        Assert.Equal(2, _viewModel.PendingCount);
        Assert.Contains("server down", _viewModel.SyncText);
    }

    [Fact]
    public void SimulateOffline_SwitchesTheSimulator()
    {
        _viewModel.SimulateOffline = true;
        Assert.True(_offline.IsOffline);

        _viewModel.SimulateOffline = false;
        Assert.False(_offline.IsOffline);
    }

    [Fact]
    public async Task ShareDatabase_ReportsSuccessAndFailure()
    {
        await _viewModel.ShareDatabaseCommand.ExecuteAsync(null);
        Assert.Equal(1, _exporter.Shares);
        Assert.Contains("share sheet", _viewModel.ExportText);

        _exporter.Failure = new IOException("disk full");
        await _viewModel.ShareDatabaseCommand.ExecuteAsync(null);
        Assert.Contains("disk full", _viewModel.ExportText);
    }
}
