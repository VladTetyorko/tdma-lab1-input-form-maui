namespace Mobile_apps_L1_Model_view.Services;

// The only class that touches Shell.Current
public sealed class ShellNavigationService : INavigationService
{
    public string CurrentRoute => Routes.Leaf(Shell.Current?.CurrentState?.Location?.OriginalString);

    public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);

    public Task GoToAsync(string route, IDictionary<string, object> parameters) =>
        Shell.Current.GoToAsync(route, parameters);
}
