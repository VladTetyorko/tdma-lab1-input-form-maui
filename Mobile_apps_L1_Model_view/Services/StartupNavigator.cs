namespace Mobile_apps_L1_Model_view.Services;

public sealed class StartupNavigator(
    IAppStateService appState,
    IDeepLinkHandler deepLinks,
    IProductCatalog catalog,
    INavigationService navigation) : IStartupNavigator
{
    public async Task RunAsync()
    {
        // Taken first so it is used (or dropped) exactly once
        var resumeRoute = appState.TakeResumeRoute();
        if (await deepLinks.HandlePendingAsync())
        {
            return;
        }

        switch (resumeRoute)
        {
            case Routes.ProductForm when appState.GetDraft<ProductFormDraft>() is not null:
                await navigation.GoToAsync(Routes.ProductForm);
                break;
            case Routes.Operation when appState.GetDraft<OperationDraft>() is { } operation:
                await OpenForProductAsync(Routes.Operation, operation.Barcode);
                break;
            case Routes.ProductDetail when appState.GetDraft<ProductDetailDraft>() is { } detail:
                await OpenForProductAsync(Routes.ProductDetail, detail.Barcode);
                break;
        }
    }

    // The page's own ViewModel then restores the draft it finds in IAppStateService
    private async Task OpenForProductAsync(string route, string barcode)
    {
        if (await catalog.GetSummaryByBarcodeAsync(barcode) is { } product)
        {
            await navigation.GoToAsync(route, new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = product });
        }
    }
}
