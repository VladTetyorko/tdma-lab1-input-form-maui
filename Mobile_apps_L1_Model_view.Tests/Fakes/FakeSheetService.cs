using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Shows nothing: remembers every sheet and lets a test act on it as the user would. By default the sheet is accepted
// (its main action runs); a sheet whose main action stays unavailable is dismissed
public sealed class FakeSheetService : ISheetService
{
    public List<SheetViewModel> Shown { get; } = [];

    public Func<SheetViewModel, Task> OnShow { get; set; } = AcceptAsync;

    public async Task<bool> ShowAsync(SheetViewModel sheet)
    {
        Shown.Add(sheet);
        await OnShow(sheet);
        sheet.Dismiss();
        return sheet.Accepted;
    }

    public static async Task AcceptAsync(SheetViewModel sheet)
    {
        if (sheet.PrimaryCommand.CanExecute(null))
        {
            await sheet.PrimaryCommand.ExecuteAsync(null);
        }
    }

    public static Task DismissAsync(SheetViewModel sheet)
    {
        sheet.Dismiss();
        return Task.CompletedTask;
    }
}
