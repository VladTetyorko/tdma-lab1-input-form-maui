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
    public const string Settings = "settings";
    public const string Logs = "logs";
    public const string Map = "map";
    public const string Receive = "receive";
    public const string Issue = "issue";
    public const string Search = "search";
    public const string Activity = "activity";
    public const string StockCount = "stockcount";
    public const string Back = "..";

    // The pages of the bottom bar: they show the menu button and the bar, every other page shows Back
    public static bool IsRoot(string? route) => route is Home or Products or Search or Activity;

    // "//products/productform" -> "productform": the page that is open, without the route path to it
    public static string Leaf(string? location) =>
        location is null ? string.Empty : location[(location.LastIndexOf('/') + 1)..];

    // Root pages are Shell content: Shell only reaches them by an absolute route. Every other page is pushed on the
    // page that is open, so ".." returns to where a task was started from
    public static string ForShell(string route) => IsRoot(route) ? Absolute(route) : route;

    // Absolute navigation (replaces the page stack)
    public static string Absolute(string route) => "//" + route;
}
