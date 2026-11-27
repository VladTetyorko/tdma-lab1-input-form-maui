using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

public partial class ProductWizardPage : ContentPage, IQueryAttributable
{
    private readonly ProductWizardViewModel _viewModel;

    public ProductWizardPage(ProductWizardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // On a phone the camera preview makes room while the code is typed
        if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
        {
            BarcodeField.InputFocusChanged += (_, _) =>
                Dispatcher.Dispatch(() => _viewModel.Form.IsTyping = BarcodeField.IsInputFocused);
        }
    }

    // The Shell hands navigation parameters to the page; the ViewModel (in Core) receives them
    public void ApplyQueryAttributes(IDictionary<string, object> query) => _viewModel.ApplyQueryAttributes(query);

    // Hardware back button: one step back, like the arrow of the app bar
    protected override bool OnBackButtonPressed()
    {
        _viewModel.BackCommand.Execute(null);
        return true;
    }
}
