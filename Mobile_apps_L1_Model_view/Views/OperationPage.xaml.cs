using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

public partial class OperationPage : ContentPage, IQueryAttributable
{
    private readonly OperationViewModel _viewModel;

    public OperationPage(OperationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // The units are what is asked for: the cursor is there, with the number selected
        Loaded += (_, _) => Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), () =>
        {
            QuantityField.FocusInput();
            QuantityField.Entry.CursorPosition = 0;
            QuantityField.Entry.SelectionLength = QuantityField.Entry.Text?.Length ?? 0;
        });
    }

    // The Shell hands navigation parameters to the page; the ViewModel (in Core) receives them
    public void ApplyQueryAttributes(IDictionary<string, object> query) => _viewModel.ApplyQueryAttributes(query);

    // Hardware back button: the same command as the on-screen back arrow and Cancel
    protected override bool OnBackButtonPressed()
    {
        _viewModel.GoBackCommand.Execute(null);
        return true;
    }
}
