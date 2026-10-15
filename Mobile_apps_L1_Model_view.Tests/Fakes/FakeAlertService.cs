using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Remembers the messages that would have been shown
public sealed class FakeAlertService : IAlertService
{
    public List<(string Title, string Message)> Shown { get; } = [];

    // The answer ConfirmAsync gives, and the questions asked
    public bool ConfirmAnswer { get; set; } = true;

    public List<(string Title, string Message)> Confirmations { get; } = [];

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        Confirmations.Add((title, message));
        return Task.FromResult(ConfirmAnswer);
    }

    public Task ShowAsync(string title, string message)
    {
        Shown.Add((title, message));
        return Task.CompletedTask;
    }
}
