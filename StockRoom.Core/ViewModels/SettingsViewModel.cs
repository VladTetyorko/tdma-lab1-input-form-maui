using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

public partial class SettingsViewModel(
    IThemeService themeService,
    IDeepLinkHandler deepLinks,
    ISyncService sync,
    IOfflineSimulator offline,
    IDatabaseExporter exporter,
    IDiagnosticsService diagnostics,
    IBiometricSettings biometricSettings,
    IBiometricAuthenticator biometrics,
    IShakeSettings shakeSettings,
    IShakeDetector shakeDetector,
    IScanSoundSettings soundSettings,
    INavigationService navigation,
    ILogger<SettingsViewModel> logger) : ObservableObject, IPageLifecycleAware
{
    public IReadOnlyList<ThemeSetting> ThemeOptions { get; } = Enum.GetValues<ThemeSetting>();

    [ObservableProperty]
    public partial ThemeSetting SelectedTheme { get; set; } = themeService.Current;

    public bool IsSystemTheme => SelectedTheme == ThemeSetting.System;

    public bool IsLightTheme => SelectedTheme == ThemeSetting.Light;

    public bool IsDarkTheme => SelectedTheme == ThemeSetting.Dark;

    [RelayCommand]
    private void SelectTheme(ThemeSetting theme) => SelectedTheme = theme;

    // Diagnostics are collapsed until asked for
    [ObservableProperty]
    public partial bool IsDiagnosticsOpen { get; set; }

    [RelayCommand]
    private void ToggleDiagnostics() => IsDiagnosticsOpen = ShowDiagnostics && !IsDiagnosticsOpen;

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

    // "Biometric unlock": on = after a restart the app asks for the fingerprint (or password) instead of opening silently
    [ObservableProperty]
    public partial bool BiometricUnlock { get; set; } = biometricSettings.IsEnabled;

    // The switch is shown only on a device that can check a fingerprint / face / Windows Hello
    [ObservableProperty]
    public partial bool IsBiometricSupported { get; set; }

    // "Shake to scan": on = shaking the device on the product list opens the scanner. On by default
    [ObservableProperty]
    public partial bool ShakeToScan { get; set; } = shakeSettings.IsEnabled;

    // "Scan sound": a beep for an accepted scan, another for a refused one. On by default
    [ObservableProperty]
    public partial bool ScanSound { get; set; } = soundSettings.IsEnabled;

    // The switch is disabled, with a note, on a device without an accelerometer (a Windows PC)
    public bool IsShakeSupported => shakeDetector.IsSupported;

    public bool IsShakeUnsupported => !shakeDetector.IsSupported;

    // The Diagnostics section is for the Manager (also refused by IDiagnosticsService)
    public bool ShowDiagnostics => diagnostics.IsAvailable;

    // "Send test crash" needs a configured crash service
    public bool CanSendTestCrash => diagnostics.CanSendTestCrash;

    [ObservableProperty]
    public partial string DiagnosticsText { get; set; } = string.Empty;

    partial void OnSelectedThemeChanged(ThemeSetting value)
    {
        themeService.Set(value);
        OnPropertyChanged(nameof(IsSystemTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));
        logger.LogInformation("Theme set to {Theme}", value);
    }

    partial void OnBiometricUnlockChanged(bool value)
    {
        biometricSettings.IsEnabled = value;
        logger.LogInformation("Biometric unlock switched {State}", value ? "on" : "off");
    }

    partial void OnShakeToScanChanged(bool value)
    {
        shakeSettings.IsEnabled = value;
        logger.LogInformation("Shake to scan switched {State}", value ? "on" : "off");
    }

    partial void OnScanSoundChanged(bool value)
    {
        soundSettings.IsEnabled = value;
        logger.LogInformation("Scan sound switched {State}", value ? "on" : "off");
    }

    partial void OnSimulateOfflineChanged(bool value)
    {
        offline.IsOffline = value;
        logger.LogInformation("Simulated offline mode switched {State}", value ? "on" : "off");
        NetworkText = DescribeNetwork();
    }

    public void OnAppearing()
    {
        sync.Completed += OnSyncCompleted;
        NetworkText = DescribeNetwork();
        OnPropertyChanged(nameof(ShowDiagnostics));
        OnPropertyChanged(nameof(CanSendTestCrash));
        RefreshPendingCommand.Execute(null);
        CheckBiometricCommand.Execute(null);
    }

    public void OnDisappearing() => sync.Completed -= OnSyncCompleted;

    [RelayCommand]
    private async Task CheckBiometricAsync() => IsBiometricSupported = await biometrics.IsAvailableAsync();

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
            logger.LogWarning("The database export was not shared, the user was told");
        }
    }

    [RelayCommand]
    private Task OpenLogsAsync() => navigation.GoToAsync(Routes.Logs);

    [RelayCommand]
    private void WriteTestEntries() => DiagnosticsText = Describe(diagnostics.WriteTestEntries());

    [RelayCommand]
    private async Task ThrowInBackgroundTaskAsync() => DiagnosticsText = Describe(await diagnostics.ThrowInBackgroundTaskAsync());

    [RelayCommand(CanExecute = nameof(CanSendTestCrash))]
    private void SendTestCrash() => DiagnosticsText = Describe(diagnostics.SendTestCrash());

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

    private static string Describe(Result<string> result) => result.IsSuccess ? result.Value! : result.Error!;

    private string DescribeNetwork() => sync.IsOnline ? "Online" : "Offline";
}
