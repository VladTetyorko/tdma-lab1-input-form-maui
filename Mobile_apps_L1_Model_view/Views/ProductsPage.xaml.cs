using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

public partial class ProductsPage : ContentPage
{
    private readonly ProductListViewModel _viewModel;

    public ProductsPage(ProductListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.EnsureLoadedCommand.Execute(null);
    }
}
