namespace Mobile_apps_L1_Model_view.Services;

// A message the user has to dismiss, for code that has no page of its own (deep links)
public interface IAlertService
{
    Task ShowAsync(string title, string message);
}
