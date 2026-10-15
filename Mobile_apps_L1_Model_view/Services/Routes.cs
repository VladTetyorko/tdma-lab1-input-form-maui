namespace Mobile_apps_L1_Model_view.Services;

// Shell routes in one place, shared by AppShell registration and ViewModels
public static class Routes
{
    public const string Login = "login";
    public const string Products = "products";
    public const string ProductForm = "productform";
    public const string ProductDetail = "productdetail";
    public const string Characteristics = "characteristics";
    public const string Operation = "operation";
    public const string Back = "..";

    // Absolute navigation (replaces the page stack)
    public static string Absolute(string route) => "//" + route;
}
