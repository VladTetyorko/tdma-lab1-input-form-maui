using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

public partial class ProductFormPage : ContentPage, IQueryAttributable
{
    private readonly ProductFormViewModel _viewModel;

    public ProductFormPage(ProductFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // The Shell hands navigation parameters to the page; the ViewModel (in Core) receives them
    public void ApplyQueryAttributes(IDictionary<string, object> query) => _viewModel.ApplyQueryAttributes(query);

    // Hardware back button: the same command as the on-screen back arrow and Cancel
    protected override bool OnBackButtonPressed()
    {
        _viewModel.CancelCommand.Execute(null);
        return true;
    }
}
