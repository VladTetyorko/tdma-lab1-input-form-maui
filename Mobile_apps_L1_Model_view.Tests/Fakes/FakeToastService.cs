using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Remembers every toast; Action is the last offered action, so a test can "tap" it
public sealed class FakeToastService : IToastService
{
    public List<string> Messages { get; } = [];

    public Action? Action { get; private set; }

    public Task ShowAsync(string message)
    {
        Messages.Add(message);
        Action = null;
        return Task.CompletedTask;
    }

    public Task ShowAsync(string message, string actionText, Action action)
    {
        Messages.Add(message);
        Action = action;
        return Task.CompletedTask;
    }
}
