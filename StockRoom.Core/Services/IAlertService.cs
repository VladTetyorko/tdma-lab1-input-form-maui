namespace Mobile_apps_L1_Model_view.Services;

// Messages and questions for the user, for code that has no page of its own (deep links, confirmations)
public interface IAlertService
{
    Task ShowAsync(string title, string message);

    // True when the user chose the accept button
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}
