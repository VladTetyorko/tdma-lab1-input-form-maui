using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class LogsViewModelTests
{
    private readonly TestStore _store = new();
    private readonly FakeLogReader _reader = new();
    private readonly LogsViewModel _viewModel;

    public LogsViewModelTests()
    {
        _viewModel = new LogsViewModel(_reader, _store.CurrentUser, _store.Logger<LogsViewModel>());
    }

    private static LogEntry Entry(LogLevel level, string message) =>
        new(DateTimeOffset.UnixEpoch, level, "A.Source", message);

    [Fact]
    public void Levels_OffersDebugToCritical_AndStartsAtInformation()
    {
        Assert.Equal([LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical],
            _viewModel.Levels);
        Assert.Equal(LogLevel.Information, _viewModel.SelectedLevel);
    }

    [Fact]
    public async Task OnAppearing_AsManager_ListsTheEntriesAtTheSelectedLevelAndAbove()
    {
        await _store.SignInAsync(Role.Manager);
        _reader.Entries.AddRange([Entry(LogLevel.Error, "newest"), Entry(LogLevel.Debug, "noise"), Entry(LogLevel.Information, "older")]);

        _viewModel.OnAppearing();

        Assert.Equal(["newest", "older"], _viewModel.Entries.Select(e => e.Message));
        Assert.Equal(string.Empty, _viewModel.EmptyText);
        Assert.Equal([LogLevel.Information], _reader.RequestedLevels);
    }

    [Fact]
    public async Task ChangingTheLevel_ReloadsWithTheNewMinimum()
    {
        await _store.SignInAsync(Role.Manager);
        _reader.Entries.AddRange([Entry(LogLevel.Error, "error"), Entry(LogLevel.Information, "info")]);
        await _viewModel.RefreshCommand.ExecuteAsync(null);
        Assert.Equal(2, _viewModel.Entries.Count);

        _viewModel.SelectedLevel = LogLevel.Error;

        Assert.Equal("error", Assert.Single(_viewModel.Entries).Message);
        Assert.Equal(LogLevel.Error, _reader.RequestedLevels.Last());
    }

    [Fact]
    public async Task Refresh_WithNothingLogged_ShowsTheEmptyState()
    {
        await _store.SignInAsync(Role.Manager);

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.Entries);
        Assert.Equal(LogsViewModel.EmptyMessage, _viewModel.EmptyText);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task Refresh_WithoutThePermission_RefusesWithAMessageAndAWarning(Role role)
    {
        await _store.SignInAsync(role);
        _reader.Entries.Add(Entry(LogLevel.Error, "secret"));

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.Entries);
        Assert.Equal(LogsViewModel.DeniedMessage, _viewModel.EmptyText);
        Assert.Empty(_reader.RequestedLevels);
        var warning = Assert.Single(_store.Logs, r => r.Level == LogLevel.Warning);
        Assert.Contains(role.ToString(), warning.Message);
    }

    [Fact]
    public async Task Refresh_NobodySignedIn_Refuses()
    {
        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(LogsViewModel.DeniedMessage, _viewModel.EmptyText);
    }

    [Fact]
    public async Task Refresh_WhenTheFilesCannotBeRead_ShowsAMessageAndLogsAnError()
    {
        await _store.SignInAsync(Role.Manager);
        _reader.Failure = new IOException("locked");

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal("The log files cannot be read.", _viewModel.EmptyText);
        Assert.Single(_store.Logs, r => r.Level == LogLevel.Error && r.Exception is IOException);
    }
}
