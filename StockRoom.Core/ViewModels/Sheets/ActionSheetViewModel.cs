using CommunityToolkit.Mvvm.Input;

namespace Mobile_apps_L1_Model_view.ViewModels.Sheets;

// One entry of a ⋮ menu. Id is also its AutomationId
public sealed record SheetAction(string Id, string Text, bool IsDestructive = false);

// The ⋮ menu of a page: the rare actions as a list; choosing one closes the sheet
public sealed partial class ActionSheetViewModel : SheetViewModel
{
    public ActionSheetViewModel(string title, params SheetAction[] actions)
    {
        Title = title;
        Actions = actions;
        SecondaryText = "Cancel";
    }

    public IReadOnlyList<SheetAction> Actions { get; }

    // The id of the chosen action, or null when the sheet was just closed
    public string? Chosen { get; private set; }

    [RelayCommand]
    private void Choose(SheetAction? action)
    {
        if (action is null || !Actions.Contains(action))
        {
            return;
        }

        Chosen = action.Id;
        Close(true);
    }
}
