using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;
using Mobile_apps_L1_Model_view.Views;
using Mobile_apps_L1_Model_view.Views.Components;

namespace Mobile_apps_L1_Model_view;

// Routes, the three root pages and the drawer. What the drawer offers to a role is DrawerViewModel's job
public partial class AppShell : Shell
{
    private readonly DrawerViewModel _drawer;

    // startRoute: the root page to show first (Routes.Login or Routes.Home)
    public AppShell(IServiceProvider services, string startRoute = Routes.Login)
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.ProductForm, typeof(ProductFormPage));
        Routing.RegisterRoute(Routes.ProductDetail, typeof(ProductDetailPage));
        Routing.RegisterRoute(Routes.Characteristics, typeof(CharacteristicsPage));
        Routing.RegisterRoute(Routes.Operation, typeof(OperationPage));
        Routing.RegisterRoute(Routes.Scanner, typeof(ScannerPage));
        Routing.RegisterRoute(Routes.Logs, typeof(LogsPage));
        Routing.RegisterRoute(Routes.Map, typeof(MapPage));
        Routing.RegisterRoute(Routes.Receive, typeof(ReceivePage));
        Routing.RegisterRoute(Routes.Issue, typeof(IssuePage));

        AddRootPage("Home", Routes.Home, services.GetRequiredService<HomePage>);
        AddRootPage("Products", Routes.Products, services.GetRequiredService<ProductsPage>);
        AddRootPage("Settings", Routes.Settings, services.GetRequiredService<SettingsPage>);

        _drawer = services.GetRequiredService<DrawerViewModel>();
        FlyoutContent = new DrawerContent(_drawer);
        FlyoutWidth = DrawerWidthOfThisDevice();

        var hamburger = new FontImageSource
        {
            FontFamily = "FluentIcons",
            Glyph = (string)Application.Current!.Resources["IconNavigation"],
            Size = 24
        };
        hamburger.SetDynamicResource(FontImageSource.ColorProperty, "TextPrimary");
        FlyoutIcon = hamburger;

        // Set before the Shell is shown, so there is no flash of the sign-in page
        CurrentItem = Items.FirstOrDefault(item => Contains(item, startRoute)) ?? CurrentItem;
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        _drawer.Select(Routes.Leaf(CurrentState?.Location?.OriginalString));
    }

    private static bool Contains(ShellItem item, string route) =>
        item.Items.Any(section => section.Items.Any(content => content.Route == route));

    // 320, but on a narrow phone never more than 85 % of the screen, so a strip of scrim stays to tap
    private static double DrawerWidthOfThisDevice()
    {
        var width = (double)Application.Current!.Resources["DrawerWidth"];
        var screen = DeviceDisplay.Current.MainDisplayInfo;
        return DeviceInfo.Current.Idiom == DeviceIdiom.Phone && screen.Density > 0
            ? Math.Min(width, screen.Width / screen.Density * 0.85)
            : width;
    }

    private void AddRootPage(string title, string route, Func<object> page) =>
        Items.Add(new ShellContent { Title = title, Route = route, ContentTemplate = new DataTemplate(page) });
}
