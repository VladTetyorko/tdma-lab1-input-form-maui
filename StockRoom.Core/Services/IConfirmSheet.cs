namespace Mobile_apps_L1_Model_view.Services;

// A themed bottom sheet with the action where the page's main button was
public interface IConfirmSheet
{
    // True when the user chose the action; Back and a tap outside mean no
    Task<bool> AskAsync(ConfirmRequest request);
}
