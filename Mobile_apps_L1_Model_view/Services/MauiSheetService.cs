using Mobile_apps_L1_Model_view.ViewModels.Sheets;
using Mobile_apps_L1_Model_view.Views.Sheets;

namespace Mobile_apps_L1_Model_view.Services;

// A sheet is a transparent modal page with a card at the bottom (centred on a wide window). Which content belongs to
// which ViewModel is decided here
public sealed class MauiSheetService : ISheetService
{
    private static int _open;

    // Read by PageLifecycleBehavior: the page under a sheet was not left
    public static bool IsOpen => _open > 0;

    public async Task<bool> ShowAsync(SheetViewModel sheet)
    {
        if (Shell.Current is not { } shell)
        {
            return false;
        }

        var page = new AppSheet(sheet, ContentFor(sheet));
        _open++;
        try
        {
            await shell.Navigation.PushModalAsync(page, animated: false);
            await page.Closed;
            await shell.Navigation.PopModalAsync(animated: false);
            return sheet.Accepted;
        }
        finally
        {
            _open--;
        }
    }

    private static View ContentFor(SheetViewModel sheet) => sheet switch
    {
        ConfirmSheetViewModel => new ConfirmContent(),
        ActionSheetViewModel => new ActionContent(),
        DocumentReviewViewModel => new DocumentReviewContent(),
        _ => throw new ArgumentException($"No view for {sheet.GetType().Name}", nameof(sheet))
    };
}
