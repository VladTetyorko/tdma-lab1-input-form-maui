using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels.Sheets;

// A question before something that cannot be taken back, or a message
public sealed class ConfirmSheetViewModel : SheetViewModel
{
    public ConfirmSheetViewModel(ConfirmRequest request)
    {
        Title = request.Title;
        Message = request.Message;
        Lines = request.Lines;
        PrimaryText = request.Accept;
        SecondaryText = request.Cancel;
        IsDestructive = request.IsDestructive;
    }

    public string Message { get; }

    public IReadOnlyList<string> Lines { get; }
}
