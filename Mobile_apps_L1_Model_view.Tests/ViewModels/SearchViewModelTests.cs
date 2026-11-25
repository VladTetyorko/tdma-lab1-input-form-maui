using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class SearchViewModelTests
{
    private const string Drill = "4006381333931";
    private const string Unknown = "3017620422003";

    private readonly TestStore _store = new();
    private readonly FakeScanFeedback _feedback = new();

    private async Task<SearchViewModel> OpenAsync(Role role = Role.Storekeeper)
    {
        await _store.SignInAsync(role);
        await _store.AddProductAsync("Drill", Drill);
        var viewModel = new SearchViewModel(_store.Catalog, _store.CurrentUser, _store.Guard, _store.Navigation, _store.Scanner,
            _store.Lookup, _feedback, _store.Clock, _store.QuickSearch, _store.Logger<SearchViewModel>());
        await viewModel.AppearCommand.ExecuteAsync(null);
        return viewModel;
    }

    private string? OpenedBarcode() =>
        _store.Navigation.RequestedParameters.LastOrDefault()?[NavigationKeys.SelectedProduct] is ProductSummary product
            ? product.Barcode
            : null;

    [Fact]
    public async Task TypingAName_ListsIt_AndPickingOpensTheProduct()
    {
        var viewModel = await OpenAsync();

        viewModel.QuickSearch.Query = "dri";
        viewModel.QuickSearch.PickCommand.Execute(Assert.Single(viewModel.QuickSearch.Suggestions));

        Assert.Equal(Routes.ProductDetail, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(Drill, OpenedBarcode());
    }

    [Fact]
    public async Task AScannedKnownCode_OpensTheProductAtOnce_AndRepeatsAreIgnored()
    {
        var viewModel = await OpenAsync();

        await viewModel.ScanDetectedCommand.ExecuteAsync(Drill);
        await viewModel.ScanDetectedCommand.ExecuteAsync(Drill);

        Assert.Equal(Routes.ProductDetail, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.False(viewModel.IsUnknown);
    }

    [Fact]
    public async Task AnUnknownCode_IsLookedUp_AndTheManagerCanCreateIt()
    {
        var viewModel = await OpenAsync(Role.Manager);
        _store.Lookup.Result = LookupResult.Found("Nutella", "Ferrero", "400 g", "Spreads");

        await viewModel.ScanDetectedCommand.ExecuteAsync(Unknown);

        Assert.True(viewModel.CanCreateUnknown);
        Assert.Contains("Nutella", viewModel.LookupText);
        await viewModel.CreateCommand.ExecuteAsync(null);
        Assert.Equal(Routes.ProductForm, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal("Nutella", _store.Navigation.RequestedParameters[0]![NavigationKeys.ProductName]);
    }

    [Fact]
    public async Task AnUnknownCode_OfAnotherRole_AsksForTheManager_AndCreateIsRefused()
    {
        var viewModel = await OpenAsync(Role.Driver);

        await viewModel.ScanDetectedCommand.ExecuteAsync(Unknown);
        await viewModel.CreateCommand.ExecuteAsync(null);

        Assert.False(viewModel.CanCreateUnknown);
        Assert.EndsWith("Ask the manager to add it.", viewModel.UnknownText);
        Assert.Empty(_store.Navigation.RequestedRoutes);
        Assert.Contains(_store.Logs, log => log.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Reset_ClearsTheFieldAndTheMessage()
    {
        var viewModel = await OpenAsync();
        await viewModel.ScanDetectedCommand.ExecuteAsync(Unknown);
        viewModel.QuickSearch.Query = "dri";

        viewModel.Reset();

        Assert.Equal(string.Empty, viewModel.QuickSearch.Query);
        Assert.False(viewModel.IsUnknown);
        Assert.Empty(viewModel.QuickSearch.Suggestions);
    }

    [Fact]
    public async Task Camera_RunsOnlyWhileThePageIsShownAndNobodyTypes()
    {
        _store.Scanner.IsCameraSupported = true;
        var viewModel = await OpenAsync();
        Assert.True(viewModel.IsCameraActive);

        viewModel.IsTyping = true;
        Assert.False(viewModel.IsCameraActive);
        viewModel.IsTyping = false;
        viewModel.OnDisappearing();

        Assert.False(viewModel.IsCameraActive);
    }

    [Fact]
    public async Task TextThatIsNoBarcode_IsSaid()
    {
        var viewModel = await OpenAsync();

        await viewModel.ScanDetectedCommand.ExecuteAsync("https://example.com");

        Assert.NotEqual(string.Empty, viewModel.StatusMessage);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }
}
