using Mobile_apps_L1_Model_view.Views;

namespace Mobile_apps_L1_Model_view.Services;

// The sheet is a transparent modal page with a card at the bottom
public sealed class MauiConfirmSheet : IConfirmSheet
{
    public async Task<bool> AskAsync(ConfirmRequest request)
    {
        if (Shell.Current is not { } shell)
        {
            return false;
        }

        var sheet = new ConfirmSheetPage(request);
        await shell.Navigation.PushModalAsync(sheet, animated: false);
        var accepted = await sheet.Answer;
        await shell.Navigation.PopModalAsync(animated: false);
        return accepted;
    }
}
