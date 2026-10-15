using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Records every route a ViewModel asks for, in order
public sealed class FakeNavigationService : INavigationService
{
    public List<string> RequestedRoutes { get; } = [];

    public Task GoToAsync(string route)
    {
        RequestedRoutes.Add(route);
        return Task.CompletedTask;
    }
}
