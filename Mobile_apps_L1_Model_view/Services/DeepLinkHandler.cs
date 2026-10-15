namespace Mobile_apps_L1_Model_view.Services;

public sealed class DeepLinkHandler(
    IProductCatalog catalog,
    ICurrentUserService currentUser,
    INavigationService navigation,
    IAlertService alerts) : IDeepLinkHandler
{
    private string? _pendingLink;

    public void Defer(string? link) => _pendingLink = link;

    public async Task<bool> HandlePendingAsync()
    {
        var link = _pendingLink;
        if (link is null || currentUser.CurrentUser is null)
        {
            return false;
        }

        _pendingLink = null;
        await HandleAsync(link);
        return true;
    }

    public async Task HandleAsync(string? link)
    {
        var parsed = DeepLinkParser.Parse(link);
        if (parsed is null)
        {
            await alerts.ShowAsync("Link not supported", "StockRoom can open links like stockroom://product/{barcode}.");
            return;
        }

        if (currentUser.CurrentUser is null)
        {
            _pendingLink = link;
            await alerts.ShowAsync("Sign in first", "The product will open after you sign in.");
            return;
        }

        var product = await catalog.GetSummaryByBarcodeAsync(parsed.Barcode);
        if (product is null)
        {
            await alerts.ShowAsync("Product not found", $"There is no product with barcode {parsed.Barcode}.");
            return;
        }

        await navigation.GoToAsync(Routes.ProductDetail,
            new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = product });
    }
}
