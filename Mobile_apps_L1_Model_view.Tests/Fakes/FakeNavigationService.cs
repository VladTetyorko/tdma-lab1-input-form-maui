using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Records every route a ViewModel asks for, in order, and the parameters sent with it
public sealed class FakeNavigationService : INavigationService
{
    public List<string> RequestedRoutes { get; } = [];

    // Parallel to RequestedRoutes: null when the call had no parameters
    public List<IDictionary<string, object>?> RequestedParameters { get; } = [];

    public Task GoToAsync(string route)
    {
        RequestedRoutes.Add(route);
        RequestedParameters.Add(null);
        return Task.CompletedTask;
    }

    public Task GoToAsync(string route, IDictionary<string, object> parameters)
    {
        RequestedRoutes.Add(route);
        RequestedParameters.Add(parameters);
        return Task.CompletedTask;
    }
}
