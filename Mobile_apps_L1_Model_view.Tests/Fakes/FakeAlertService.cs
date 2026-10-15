using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Remembers the messages that would have been shown
public sealed class FakeAlertService : IAlertService
{
    public List<(string Title, string Message)> Shown { get; } = [];

    public Task ShowAsync(string title, string message)
    {
        Shown.Add((title, message));
        return Task.CompletedTask;
    }
}
