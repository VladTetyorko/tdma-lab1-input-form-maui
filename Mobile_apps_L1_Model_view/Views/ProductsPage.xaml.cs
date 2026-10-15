using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

public partial class ProductsPage : ContentPage
{
    public ProductsPage(ProductListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
