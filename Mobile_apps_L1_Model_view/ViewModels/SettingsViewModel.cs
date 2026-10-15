using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

public partial class SettingsViewModel(
    IThemeService themeService,
    IDeepLinkHandler deepLinks,
    ISyncService sync,
    IOfflineSimulator offline,
    IDatabaseExporter exporter) : ObservableObject, IPageLifecycleAware
{
    public IReadOnlyList<ThemeSetting> ThemeOptions { get; } = Enum.GetValues<ThemeSetting>();

    [ObservableProperty]
    public partial ThemeSetting SelectedTheme { get; set; } = themeService.Current;

    // A stockroom:// link typed by hand: the way to try deep links where no other app can send one (Windows)
    [ObservableProperty]
    public partial string LinkText { get; set; } = string.Empty;

    // Changes saved on this device that the server does not have yet
    [ObservableProperty]
    public partial int PendingCount { get; set; }

    [ObservableProperty]
    public partial string NetworkText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SyncText { get; set; } = string.Empty;

    // Demo switch: behave as if there were no network (see IOfflineSimulator)
    [ObservableProperty]
    public partial bool SimulateOffline { get; set; } = offline.IsOffline;

    [ObservableProperty]
    public partial string ExportText { get; set; } = string.Empty;

    partial void OnSelectedThemeChanged(ThemeSetting value) => themeService.Set(value);

    partial void OnSimulateOfflineChanged(bool value)
    {
        offline.IsOffline = value;
        NetworkText = DescribeNetwork();
    }

    public void OnAppearing()
    {
        sync.Completed += OnSyncCompleted;
        NetworkText = DescribeNetwork();
        RefreshPendingCommand.Execute(null);
    }

    public void OnDisappearing() => sync.Completed -= OnSyncCompleted;

    [RelayCommand]
    private async Task RefreshPendingAsync() => PendingCount = await sync.GetPendingCountAsync();

    [RelayCommand]
    private async Task SyncNowAsync() => OnSyncCompleted(this, await sync.SyncNowAsync());

    [RelayCommand]
    private async Task ShareDatabaseAsync()
    {
        try
        {
            await exporter.ShareAsync();
            ExportText = "The database file was handed to the share sheet.";
        }
        catch (Exception ex)
        {
            ExportText = $"The database could not be shared: {ex.Message}";
        }
    }

    [RelayCommand]
    private Task OpenLinkAsync() => deepLinks.HandleAsync(LinkText);

    // A sync can finish on any thread (network change, app resume): bindings marshal property changes to the UI
    private void OnSyncCompleted(object? sender, SyncResult result)
    {
        PendingCount = result.Remaining;
        NetworkText = DescribeNetwork();
        SyncText = result.Status switch
        {
            SyncStatus.Synced => $"Synced: {result.Pushed} sent, {result.Remaining} waiting.",
            SyncStatus.Offline => $"Offline: {result.Remaining} changes wait for a connection.",
            SyncStatus.Failed => $"Sync failed ({result.Error}). {result.Remaining} changes stay queued and will be retried.",
            _ => "A sync is already running."
        };
    }

    private string DescribeNetwork() => sync.IsOnline ? "Online" : "Offline";
}
