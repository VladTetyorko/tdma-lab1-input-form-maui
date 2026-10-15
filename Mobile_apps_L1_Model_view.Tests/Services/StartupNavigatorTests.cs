using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class StartupNavigatorTests
{
    private readonly TestStore _store = new();
    private readonly string _drillBarcode = Ean13.WithCheckDigit("482000100001");

    [Fact]
    public async Task NothingToResume_NavigatesNowhere()
    {
        await _store.SignInAsync(Role.Storekeeper);
        Resume(Routes.Products);

        await _store.Startup.RunAsync();

        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task PendingDeepLink_WinsOverTheResumeRoute()
    {
        var drill = await _store.AddProductAsync("Drill", _drillBarcode);
        await _store.SignInAsync(Role.Storekeeper);
        _store.DeepLinks.Defer(DeepLinkParser.ForProduct(_drillBarcode));
        Resume(Routes.Operation, state => state.SetDraft(OperationDraftFor(_drillBarcode)));

        await _store.Startup.RunAsync();

        Assert.Equal(Routes.ProductDetail, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(drill.Id, SelectedProduct().Id);
    }

    [Fact]
    public async Task ProductFormWithDraft_ResumesTheForm()
    {
        await _store.SignInAsync(Role.Manager);
        Resume(Routes.ProductForm, state => state.SetDraft(new ProductFormDraft("Saw", _drillBarcode)));

        await _store.Startup.RunAsync();

        Assert.Equal(Routes.ProductForm, Assert.Single(_store.Navigation.RequestedRoutes));
    }

    [Fact]
    public async Task ProductFormWithoutDraft_DoesNotResume()
    {
        await _store.SignInAsync(Role.Manager);
        Resume(Routes.ProductForm);

        await _store.Startup.RunAsync();

        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task OperationWithDraft_ReopensTheOperationForTheDraftProduct()
    {
        var drill = await _store.AddProductAsync("Drill", _drillBarcode);
        await _store.SignInAsync(Role.Storekeeper);
        Resume(Routes.Operation, state => state.SetDraft(OperationDraftFor(_drillBarcode)));

        await _store.Startup.RunAsync();

        Assert.Equal(Routes.Operation, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(drill.Id, SelectedProduct().Id);
    }

    [Fact]
    public async Task OperationWithoutDraft_DoesNotResume()
    {
        await _store.AddProductAsync("Drill", _drillBarcode);
        await _store.SignInAsync(Role.Storekeeper);
        Resume(Routes.Operation);

        await _store.Startup.RunAsync();

        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task OperationDraftForDeletedProduct_DoesNotResume()
    {
        await _store.SignInAsync(Role.Storekeeper);
        Resume(Routes.Operation, state => state.SetDraft(OperationDraftFor(_drillBarcode)));

        await _store.Startup.RunAsync();

        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task ProductDetailWithDraft_ReopensTheProductDetail()
    {
        var drill = await _store.AddProductAsync("Drill", _drillBarcode);
        await _store.SignInAsync(Role.Manager);
        Resume(Routes.ProductDetail, state => state.SetDraft(
            new ProductDetailDraft(_drillBarcode, new Dictionary<Guid, string>())));

        await _store.Startup.RunAsync();

        Assert.Equal(Routes.ProductDetail, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(drill.Id, SelectedProduct().Id);
    }

    [Fact]
    public async Task ProductDetailWithoutDraft_DoesNotResume()
    {
        await _store.AddProductAsync("Drill", _drillBarcode);
        await _store.SignInAsync(Role.Manager);
        Resume(Routes.ProductDetail);

        await _store.Startup.RunAsync();

        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task DraftOfAnotherType_DoesNotResumeTheRoute()
    {
        await _store.SignInAsync(Role.Manager);
        Resume(Routes.ProductForm, state => state.SetDraft(OperationDraftFor(_drillBarcode)));

        await _store.Startup.RunAsync();

        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task ResumeRouteIsUsedOnlyOnce()
    {
        await _store.SignInAsync(Role.Manager);
        Resume(Routes.ProductForm, state => state.SetDraft(new ProductFormDraft("Saw", _drillBarcode)));

        await _store.Startup.RunAsync();
        await _store.Startup.RunAsync();

        Assert.Single(_store.Navigation.RequestedRoutes);
    }

    // Saves what a previous run left behind, then reads it back the way a new start does
    private void Resume(string route, Action<AppStateService>? seed = null)
    {
        var previous = new AppStateService(_store.Preferences) { LastRoute = route };
        seed?.Invoke(previous);
        previous.Save();
        _store.AppState.Restore();
    }

    private static OperationDraft OperationDraftFor(string barcode) =>
        new(barcode, OperationDirection.Out, 2, "Acme Ltd", "Site 7");

    private ProductSummary SelectedProduct() =>
        Assert.IsType<ProductSummary>(_store.Navigation.RequestedParameters.Last()![NavigationKeys.SelectedProduct]);
}
