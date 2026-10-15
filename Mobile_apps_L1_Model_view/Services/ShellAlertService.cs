namespace Mobile_apps_L1_Model_view.Services;

public sealed class ShellAlertService : IAlertService
{
    public Task ShowAsync(string title, string message) =>
        Shell.Current is { } shell ? shell.DisplayAlertAsync(title, message, "OK") : Task.CompletedTask;

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        Shell.Current is { } shell ? shell.DisplayAlertAsync(title, message, accept, cancel) : Task.FromResult(false);
}
