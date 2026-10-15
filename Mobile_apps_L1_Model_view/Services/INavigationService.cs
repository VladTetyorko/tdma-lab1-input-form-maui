namespace Mobile_apps_L1_Model_view.Services;

// Keeps ViewModels free of Shell: the only way a ViewModel navigates
public interface INavigationService
{
    // Leaf route of the page that is open now ("products", "productform"), empty before the first page
    string CurrentRoute { get; }

    // Shell route: "//login" (absolute), "productform" (push), ".." (back)
    Task GoToAsync(string route);

    // Same, but hands objects to the target page (see NavigationKeys). The target's ViewModel
    // receives them in IQueryAttributable.ApplyQueryAttributes; for ".." the previous page does
    Task GoToAsync(string route, IDictionary<string, object> parameters);
}
