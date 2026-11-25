using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Services;

// Messages and questions of code without a page of its own, in the same sheet as everything else
public sealed class SheetAlertService(ISheetService sheets) : IAlertService
{
    public Task ShowAsync(string title, string message) =>
        sheets.ShowAsync(new ConfirmSheetViewModel(new ConfirmRequest(title, message, "Close", string.Empty)));

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        sheets.ShowAsync(new ConfirmSheetViewModel(new ConfirmRequest(title, message, accept, cancel)));
}
