using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// The in-app log viewer (Manager only): the newest log entries at or above the chosen level, newest first
public partial class LogsViewModel(
    ILogReader reader,
    ICurrentUserService currentUser,
    ILogger<LogsViewModel> logger) : ObservableObject, IPageLifecycleAware
{
    public const string DeniedMessage = "Only a Manager can view the logs.";
    public const string EmptyMessage = "No log entries at this level yet.";

    public IReadOnlyList<LogLevel> Levels { get; } =
        [LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical];

    public ObservableCollection<LogEntry> Entries { get; } = [];

    [ObservableProperty]
    public partial LogLevel SelectedLevel { get; set; } = LogLevel.Information;

    // Why the list is empty (denied, nothing logged yet), or empty when there are entries
    [ObservableProperty]
    public partial string EmptyText { get; set; } = string.Empty;

    partial void OnSelectedLevelChanged(LogLevel value) => RefreshCommand.Execute(null);

    public void OnAppearing() => RefreshCommand.Execute(null);

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Entries.Clear();
        if (currentUser.CurrentUser is not { } user || !RolePermissions.CanViewLogs(user.Role))
        {
            EmptyText = DeniedMessage;
            logger.LogWarning("Log viewer denied: role {Role} cannot view logs", currentUser.CurrentUser?.Role);
            return;
        }

        try
        {
            foreach (var entry in await reader.ReadNewestAsync(SelectedLevel))
            {
                Entries.Add(entry);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            EmptyText = "The log files cannot be read.";
            logger.LogError(ex, "Reading the log files failed");
            return;
        }

        EmptyText = Entries.Count == 0 ? EmptyMessage : string.Empty;
    }
}
