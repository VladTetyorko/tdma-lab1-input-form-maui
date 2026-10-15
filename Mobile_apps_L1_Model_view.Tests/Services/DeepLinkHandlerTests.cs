using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class DeepLinkHandlerTests
{
    private readonly TestStore _store = new();
    private readonly string _drillBarcode = Ean13.WithCheckDigit("482000100001");

    [Fact]
    public async Task UnsupportedLink_ShowsAnAlertAndNavigatesNowhere()
    {
        await _store.SignInAsync(Role.Storekeeper);

        await _store.DeepLinks.HandleAsync("https://example.com/product/1");

        var alert = Assert.Single(_store.Alerts.Shown);
        Assert.Equal("Link not supported", alert.Title);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task NotSignedIn_KeepsTheLinkPendingAndAsksToSignIn()
    {
        await _store.AddProductAsync("Drill", _drillBarcode);

        await _store.DeepLinks.HandleAsync(DeepLinkParser.ForProduct(_drillBarcode));

        Assert.Equal("Sign in first", Assert.Single(_store.Alerts.Shown).Title);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task HandlePendingAsync_WhileSignedOut_ReturnsFalseAndKeepsTheLink()
    {
        var drill = await _store.AddProductAsync("Drill", _drillBarcode);
        await _store.DeepLinks.HandleAsync(DeepLinkParser.ForProduct(_drillBarcode));

        Assert.False(await _store.DeepLinks.HandlePendingAsync());
        Assert.False(await _store.DeepLinks.HandlePendingAsync());
        Assert.Empty(_store.Navigation.RequestedRoutes);

        await _store.SignInAsync(Role.Storekeeper);
        Assert.True(await _store.DeepLinks.HandlePendingAsync());
        Assert.Equal(drill.Id, SelectedProduct().Id);
    }

    [Fact]
    public async Task HandlePendingAsync_AfterSignIn_NavigatesToTheProductOnce()
    {
        var drill = await _store.AddProductAsync("Drill", _drillBarcode);
        await _store.DeepLinks.HandleAsync(DeepLinkParser.ForProduct(_drillBarcode));
        await _store.SignInAsync(Role.Storekeeper);

        Assert.True(await _store.DeepLinks.HandlePendingAsync());
        Assert.False(await _store.DeepLinks.HandlePendingAsync());

        Assert.Equal(Routes.ProductDetail, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(drill.Id, SelectedProduct().Id);
    }

    [Fact]
    public async Task HandlePendingAsync_WithoutDeferredLink_ReturnsFalse()
    {
        await _store.SignInAsync(Role.Storekeeper);

        Assert.False(await _store.DeepLinks.HandlePendingAsync());
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task Defer_ThenSignIn_OpensTheProductOnHandlePending()
    {
        var drill = await _store.AddProductAsync("Drill", _drillBarcode);
        _store.DeepLinks.Defer(DeepLinkParser.ForProduct(_drillBarcode));
        await _store.SignInAsync(Role.Driver);

        Assert.True(await _store.DeepLinks.HandlePendingAsync());

        Assert.Equal(Routes.ProductDetail, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(drill.Id, SelectedProduct().Id);
    }

    [Fact]
    public async Task Defer_Null_LeavesNothingPending()
    {
        _store.DeepLinks.Defer(null);
        await _store.SignInAsync(Role.Driver);

        Assert.False(await _store.DeepLinks.HandlePendingAsync());
    }

    [Fact]
    public async Task UnknownBarcode_ShowsProductNotFoundAndNavigatesNowhere()
    {
        await _store.SignInAsync(Role.Storekeeper);

        await _store.DeepLinks.HandleAsync(DeepLinkParser.ForProduct(_drillBarcode));

        var alert = Assert.Single(_store.Alerts.Shown);
        Assert.Equal("Product not found", alert.Title);
        Assert.Contains(_drillBarcode, alert.Message);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task KnownBarcode_NavigatesToProductDetailWithTheSummary()
    {
        var drill = await _store.AddProductAsync("Drill", _drillBarcode);
        await _store.Operations.AddAsync(new Operation { ProductId = drill.Id, Direction = OperationDirection.In, Quantity = 4 });
        await _store.SignInAsync(Role.Storekeeper);

        await _store.DeepLinks.HandleAsync(DeepLinkParser.ForProduct(_drillBarcode));

        Assert.Empty(_store.Alerts.Shown);
        Assert.Equal(Routes.ProductDetail, Assert.Single(_store.Navigation.RequestedRoutes));
        var summary = SelectedProduct();
        Assert.Equal(drill.Id, summary.Id);
        Assert.Equal("Drill", summary.Name);
        Assert.Equal(_drillBarcode, summary.Barcode);
        Assert.Equal(4, summary.Quantity);
    }

    private ProductSummary SelectedProduct() =>
        Assert.IsType<ProductSummary>(_store.Navigation.RequestedParameters.Last()![NavigationKeys.SelectedProduct]);
}
