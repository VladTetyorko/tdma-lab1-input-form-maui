using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Services;

// The one way the app asks something: a themed sheet that shows a SheetViewModel
public interface ISheetService
{
    // Completes when the sheet is closed; true when its main action was carried out
    Task<bool> ShowAsync(SheetViewModel sheet);
}
