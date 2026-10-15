using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Views;

namespace Mobile_apps_L1_Model_view;

public partial class AppShell : Shell
{
    // startRoute: the top-level page to show first (Routes.Login or Routes.Products)
    public AppShell(string startRoute = Routes.Login)
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.ProductForm, typeof(ProductFormPage));
        Routing.RegisterRoute(Routes.ProductDetail, typeof(ProductDetailPage));
        Routing.RegisterRoute(Routes.Characteristics, typeof(CharacteristicsPage));
        Routing.RegisterRoute(Routes.Operation, typeof(OperationPage));
        Routing.RegisterRoute(Routes.Settings, typeof(SettingsPage));
        Routing.RegisterRoute(Routes.Scanner, typeof(ScannerPage));

        // Set before the Shell is shown, so there is no flash of the sign-in page
        CurrentItem = Items.FirstOrDefault(item => item.CurrentItem?.CurrentItem?.Route == startRoute) ?? CurrentItem;
    }
}
