namespace Mobile_apps_L1_Model_view.Services;

// Keeps ViewModels free of Shell. Lab 2 extends it (parameters, going back with data).
public interface INavigationService
{
    // Shell route: "//login" (absolute), "productform" (push), ".." (back)
    Task GoToAsync(string route);
}
