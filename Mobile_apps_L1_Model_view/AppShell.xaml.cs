using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;
using Mobile_apps_L1_Model_view.Views;
using Mobile_apps_L1_Model_view.Views.Components;

namespace Mobile_apps_L1_Model_view;

// Builds the main navigation and keeps it in step with the signed-in role. What a role sees is decided by
// NavigationMenu in Core; this class only shows and hides
public partial class AppShell : Shell
{
    private readonly ICurrentUserService _currentUser;
    private readonly bool _sidePanel = DeviceInfo.Current.Idiom != DeviceIdiom.Phone;

    // Route -> the element that has to be hidden when the role does not include it
    private readonly Dictionary<string, BaseShellItem> _entries = [];
    private readonly SessionStatusViewModel _status;

    // startRoute: the top-level page to show first (Routes.Login or Routes.Products)
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

        AddMainEntries(services);
        _status = services.GetRequiredService<SessionStatusViewModel>();
        var status = _status;
        status.PropertyChanged += (_, _) => Dispatcher.Dispatch(ApplyDraftCounts);
        ApplyDraftCounts();
        if (_sidePanel)
        {
            var header = new SidePanelHeader();
            header.Content.BindingContext = status;
            var footer = new SidePanelFooter();
            footer.Content.BindingContext = status;
            FlyoutHeader = header;
            FlyoutFooter = footer;
        }

        _currentUser = services.GetRequiredService<ICurrentUserService>();
        _currentUser.Changed += (_, _) => Dispatcher.Dispatch(ApplyRole);
        ApplyRole();

        // Set before the Shell is shown, so there is no flash of the sign-in page
        CurrentItem = Items.FirstOrDefault(item => Contains(item, startRoute)) ?? CurrentItem;
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        var onLogin = CurrentState?.Location?.OriginalString.Contains(Routes.Login, StringComparison.Ordinal) ?? true;
        FlyoutBehavior = _sidePanel && !onLogin ? FlyoutBehavior.Locked : FlyoutBehavior.Disabled;
    }

    private static bool Contains(ShellItem item, string route) =>
        item.Items.Any(section => section.Items.Any(content => content.Route == route));

    private static (string Glyph, Func<object> Page) Look(IServiceProvider services, string route) => route switch
    {
        Routes.Receive => ("IconReceive", () => Document(services, OperationDirection.In)),
        Routes.Issue => ("IconIssue", () => Document(services, OperationDirection.Out)),
        Routes.Settings => ("IconSettings", services.GetRequiredService<SettingsPage>),
        _ => ("IconBox", services.GetRequiredService<ProductsPage>)
    };

    private static StockDocumentPage Document(IServiceProvider services, OperationDirection direction) =>
        new(services.GetRequiredService<StockDocumentViewModel>(), direction);

    private void AddMainEntries(IServiceProvider services)
    {
        var tabs = new TabBar();
        // The Manager sees every entry; ApplyRole hides what the signed-in role does not have
        foreach (var entry in NavigationMenu.For(Role.Manager))
        {
            var (glyph, page) = Look(services, entry.Route);
            var content = new ShellContent { Title = entry.Title, Route = entry.Route, ContentTemplate = new DataTemplate(page) };
            var icon = new FontImageSource { FontFamily = "FluentIcons", Glyph = (string)Application.Current!.Resources[glyph], Size = 20 };
            icon.SetDynamicResource(FontImageSource.ColorProperty, "TextSecondary");

            if (_sidePanel)
            {
                var item = new FlyoutItem { Title = entry.Title, Icon = icon };
                item.Items.Add(content);
                Items.Add(item);
                _entries[entry.Route] = item;
            }
            else
            {
                var tab = new Tab { Title = entry.Title, Icon = icon };
                tab.Items.Add(content);
                tabs.Items.Add(tab);
                _entries[entry.Route] = tab;
            }
        }

        if (!_sidePanel)
        {
            Items.Add(tabs);
        }
    }

    // A half-done delivery is never forgotten: "Receive (5)"
    private void ApplyDraftCounts()
    {
        SetTitle(Routes.Receive, _status.ReceiveTitle);
        SetTitle(Routes.Issue, _status.IssueTitle);
    }

    private void SetTitle(string route, string title)
    {
        if (_entries.TryGetValue(route, out var item))
        {
            item.Title = title;
        }
    }

    // After sign-out the last menu stays: only the sign-in page is on screen then
    private void ApplyRole()
    {
        if (_currentUser.CurrentRole is not { } role)
        {
            return;
        }

        var allowed = NavigationMenu.For(role).Select(entry => entry.Route).ToHashSet();
        foreach (var (route, item) in _entries)
        {
            item.IsVisible = allowed.Contains(route);
        }
    }
}
