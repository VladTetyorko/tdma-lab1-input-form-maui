using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

// One page for both batch directions, created as ReceivePage or IssuePage. The code here is about focus only
public partial class StockDocumentPage : ContentPage
{
    private readonly StockDocumentViewModel _viewModel;
    private readonly bool _phone = DeviceInfo.Current.Idiom == DeviceIdiom.Phone;

    public StockDocumentPage(StockDocumentViewModel viewModel, OperationDirection direction)
    {
        viewModel.Direction = direction;
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        Search.InputFocusChanged += OnTypingFocusChanged;
        Quantity.InputFocusChanged += OnTypingFocusChanged;

        // A keyboard scanner types into the box that has the focus: give it back after every Enter
        Search.InputCompleted += (_, _) => FocusSearch();
        viewModel.QuickSearch.Picked += (_, _) => AfterPick();

        // Without a camera, typing is the main way in
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(StockDocumentViewModel.CameraState)
                && viewModel.CameraState is CameraState.Unsupported or CameraState.Denied or CameraState.Failed)
            {
                FocusSearch();
            }
        };
    }

    private void FocusSearch() => Dispatcher.Dispatch(Search.FocusInput);

    // On a phone with a working camera the keyboard goes away after a pick, so the preview comes back
    private void AfterPick()
    {
        if (_phone && _viewModel.CameraState == CameraState.Active)
        {
            Dispatcher.Dispatch(Search.UnfocusInput);
        }
        else
        {
            FocusSearch();
        }
    }

    // Dispatched, so moving from one box to the other does not count as "stopped typing"
    private void OnTypingFocusChanged(object? sender, EventArgs e)
    {
        if (_phone)
        {
            Dispatcher.Dispatch(() => _viewModel.IsTyping = Search.IsInputFocused || Quantity.IsInputFocused);
        }
    }
}
