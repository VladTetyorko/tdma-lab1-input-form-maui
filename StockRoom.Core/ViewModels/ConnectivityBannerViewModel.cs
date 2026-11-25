using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// "Offline / Syncing... / 3 changes waiting / All changes synced", reusable by any page (DRY). It listens to the events
// that already exist (network change, "Simulate offline", sync started and completed): nothing is polled.
// Attach in OnAppearing and Detach in OnDisappearing, so a page that is not shown does not keep listening.
// Auto-sync on reconnect is SyncService's job (since Lab 4); this only shows the result
public sealed partial class ConnectivityBannerViewModel(
    ISyncService sync,
    IOfflineSimulator offline,
    IConnectivityMonitor connectivity,
    ILogger<ConnectivityBannerViewModel> logger) : ObservableObject
{
    private bool _attached;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOffline))]
    [NotifyPropertyChangedFor(nameof(IsNotOffline))]
    [NotifyPropertyChangedFor(nameof(NeedsAttention))]
    public partial ConnectivityState State { get; set; } = ConnectivityState.Synced;

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    // The banner is drawn in the warning color only while offline
    public bool IsOffline => State == ConnectivityState.Offline;

    public bool IsNotOffline => !IsOffline;

    // Anything but "all synced": pages show the chip only then
    public bool NeedsAttention => State != ConnectivityState.Synced;

    public void Attach()
    {
        if (_attached)
        {
            return;
        }

        _attached = true;
        sync.Started += OnSyncStarted;
        sync.Completed += OnSyncCompleted;
        offline.Changed += OnNetworkChanged;
        connectivity.Changed += OnNetworkChanged;
        RefreshCommand.Execute(null);
    }

    public void Detach()
    {
        if (!_attached)
        {
            return;
        }

        _attached = false;
        sync.Started -= OnSyncStarted;
        sync.Completed -= OnSyncCompleted;
        offline.Changed -= OnNetworkChanged;
        connectivity.Changed -= OnNetworkChanged;
    }

    // Reads the current state: offline, syncing, or the length of the queue
    [RelayCommand]
    private async Task RefreshAsync()
    {
        // Runs from events, with nobody awaiting it: a queue that cannot be read must not end the app
        try
        {
            await ReadStateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "The sync state could not be read");
        }
    }

    private async Task ReadStateAsync()
    {
        if (!sync.IsOnline)
        {
            Show(ConnectivityState.Offline, await WaitingTextAsync("Offline"));
        }
        else if (sync.IsSyncing)
        {
            Show(ConnectivityState.Syncing, "Syncing...");
        }
        else
        {
            ShowQueue(await sync.GetPendingCountAsync());
        }
    }

    private void OnSyncStarted(object? sender, EventArgs e) => Show(ConnectivityState.Syncing, "Syncing...");

    private void OnSyncCompleted(object? sender, SyncResult result)
    {
        if (!sync.IsOnline || result.Status == SyncStatus.Offline)
        {
            Show(ConnectivityState.Offline, WaitingText("Offline", result.Remaining));
        }
        else
        {
            ShowQueue(result.Remaining);
        }
    }

    // The network or the simulator switch changed: offline is shown at once, online asks the queue (a sync usually follows)
    private void OnNetworkChanged(object? sender, EventArgs e) => RefreshCommand.Execute(null);

    private void ShowQueue(int pending) =>
        Show(
            pending > 0 ? ConnectivityState.Pending : ConnectivityState.Synced,
            pending > 0 ? $"{pending} {(pending == 1 ? "change" : "changes")} waiting to sync" : "All changes synced");

    private async Task<string> WaitingTextAsync(string prefix) => WaitingText(prefix, await sync.GetPendingCountAsync());

    private static string WaitingText(string prefix, int pending) =>
        pending > 0 ? $"{prefix}: {pending} {(pending == 1 ? "change" : "changes")} waiting for a connection" : prefix;

    private void Show(ConnectivityState state, string text)
    {
        if (state != State)
        {
            logger.LogDebug("Connectivity banner: {State}", state);
        }

        State = state;
        Text = text;
    }
}
