using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Services;

// A yes / no question as a sheet
public sealed class SheetConfirm(ISheetService sheets) : IConfirmSheet
{
    public Task<bool> AskAsync(ConfirmRequest request) => sheets.ShowAsync(new ConfirmSheetViewModel(request));
}
