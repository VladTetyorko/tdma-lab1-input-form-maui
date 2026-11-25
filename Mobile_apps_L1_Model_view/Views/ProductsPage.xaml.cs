using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

public partial class ProductsPage : ContentPage, IQueryAttributable
{
    private readonly ProductListViewModel _viewModel;

    public ProductsPage(ProductListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Below this width the table keeps only name, quantity and one chosen characteristic
    private const double NarrowWidth = 600;

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
        {
            _viewModel.IsNarrow = width < NarrowWidth;
        }
    }

    // The Shell hands navigation parameters to the page; the ViewModel (in Core) receives them
    public void ApplyQueryAttributes(IDictionary<string, object> query) => _viewModel.ApplyQueryAttributes(query);
}
