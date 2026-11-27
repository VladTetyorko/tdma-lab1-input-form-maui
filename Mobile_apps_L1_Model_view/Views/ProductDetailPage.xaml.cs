using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

public partial class ProductDetailPage : ContentPage, IQueryAttributable
{
    private readonly ProductDetailViewModel _viewModel;

    public ProductDetailPage(ProductDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // The Shell hands navigation parameters to the page; the ViewModel (in Core) receives them
    public void ApplyQueryAttributes(IDictionary<string, object> query) => _viewModel.ApplyQueryAttributes(query);

    // Hardware back button: the same command as the on-screen back arrow and button
    protected override bool OnBackButtonPressed()
    {
        _viewModel.GoBackCommand.Execute(null);
        return true;
    }
}
