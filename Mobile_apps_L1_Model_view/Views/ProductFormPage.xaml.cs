using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

public partial class ProductFormPage : ContentPage
{
    public ProductFormPage(ProductFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
