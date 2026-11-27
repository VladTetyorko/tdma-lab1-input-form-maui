namespace Mobile_apps_L1_Model_view.Services;

// A short, non-blocking message, optionally with one action. Supplementary only: a page never relies on it to show
// state or to offer the single way to do something, because not every platform can show it
public interface IToastService
{
    Task ShowAsync(string message);

    Task ShowAsync(string message, string actionText, Action action);
}
