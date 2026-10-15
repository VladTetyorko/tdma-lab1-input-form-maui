using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Views;

namespace Mobile_apps_L1_Model_view;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.ProductForm, typeof(ProductFormPage));
        Routing.RegisterRoute(Routes.ProductDetail, typeof(ProductDetailPage));
        Routing.RegisterRoute(Routes.Characteristics, typeof(CharacteristicsPage));
        Routing.RegisterRoute(Routes.Operation, typeof(OperationPage));
    }
}
