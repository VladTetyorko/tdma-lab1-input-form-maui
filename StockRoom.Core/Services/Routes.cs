namespace Mobile_apps_L1_Model_view.Services;

// Shell routes in one place, shared by AppShell registration and ViewModels
public static class Routes
{
    public const string Login = "login";
    public const string Home = "home";
    public const string Products = "products";
    public const string ProductForm = "productform";
    public const string ProductDetail = "productdetail";
    public const string Characteristics = "characteristics";
    public const string Operation = "operation";
    public const string Scanner = "scanner";
    public const string Settings = "settings";
    public const string Logs = "logs";
    public const string Map = "map";
    public const string Receive = "receive";
    public const string Issue = "issue";
    public const string Back = "..";

    // "//products/productform" -> "productform": the page that is open, without the route path to it
    public static string Leaf(string? location) =>
        location is null ? string.Empty : location[(location.LastIndexOf('/') + 1)..];

    // Home, Products and Settings are Shell content: Shell only reaches them by an absolute route. Receive and Issue
    // are pushed on Home, so that Back returns Home instead of leaving the app
    public static string ForShell(string route) => route switch
    {
        Home or Products or Settings => Absolute(route),
        Receive or Issue => $"{Absolute(Home)}/{route}",
        _ => route
    };

    // Absolute navigation (replaces the page stack)
    public static string Absolute(string route) => "//" + route;
}
