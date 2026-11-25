using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.EndToEnd;

// Batch receive and issue through the ViewModels, the real services and a real SQLite file
public class StockDocumentFlowTests
{
    private static async Task<User> SignInAsync(ServiceProvider services, Role role)
    {
        var user = (await services.GetRequiredService<IUserRepository>().GetAllAsync()).First(u => u.Role == role);
        services.GetRequiredService<ICurrentUserService>().SignIn(user);
        return user;
    }

    private static async Task<StockDocumentViewModel> OpenAsync(ServiceProvider services, OperationDirection direction)
    {
        var viewModel = services.GetRequiredService<StockDocumentViewModel>();
        viewModel.Direction = direction;
        await viewModel.LoadCommand.ExecuteAsync(null);
        return viewModel;
    }

    private static async Task TypeAsync(StockDocumentViewModel viewModel, string barcode)
    {
        viewModel.QuickSearch.Query = barcode;
        await viewModel.AddTypedCommand.ExecuteAsync(null);
    }

    [Fact]
    public async Task DriverReceivesThreeProducts_TheListShowsTheNewQuantities()
    {
        using var services = TestContainer.Create(new FakePreferencesStore(), new FakeNavigationService());
        await SignInAsync(services, Role.Driver);
        var list = services.GetRequiredService<ProductListViewModel>();
        await list.EnsureLoadedCommand.ExecuteAsync(null);
        var three = list.Products.Take(3).ToList();
        var receive = await OpenAsync(services, OperationDirection.In);
        foreach (var product in three)
        {
            await TypeAsync(receive, product.Barcode);
            await TypeAsync(receive, product.Barcode);
        }

        await receive.CommitCommand.ExecuteAsync(null);
        await list.EnsureLoadedCommand.ExecuteAsync(null);

        foreach (var product in three)
        {
            Assert.Equal(product.Quantity + 2, list.Products.Single(p => p.Id == product.Id).Quantity);
        }
    }

    [Fact]
    public async Task StorekeeperIssuesTooMany_IsRefusedAndNothingIsWritten()
    {
        using var services = TestContainer.Create(new FakePreferencesStore(), new FakeNavigationService());
        await SignInAsync(services, Role.Storekeeper);
        var product = (await services.GetRequiredService<IProductCatalog>().GetSummariesAsync())[0];
        var before = (await services.GetRequiredService<IOperationRepository>().GetAllAsync()).Count;
        var draft = new StockDocumentDraft(OperationDirection.Out);
        draft.Add(product.Barcode, product.Quantity + 1);

        var result = await services.GetRequiredService<IStockDocumentService>().CommitAsync(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal(before, (await services.GetRequiredService<IOperationRepository>().GetAllAsync()).Count);
        Assert.Empty(await services.GetRequiredService<IStockDocumentRepository>().GetRecentAsync(5));
    }

    // Bottom bar -> Receive -> type "nut" -> pick -> quantity 12 -> commit -> the product list shows the new quantity
    [Fact]
    public async Task StorekeeperReceivesFromHomeByPickingASuggestion()
    {
        var navigation = new FakeNavigationService();
        using var services = TestContainer.Create(new FakePreferencesStore(), navigation);
        await SignInAsync(services, Role.Storekeeper);
        var bar = services.GetRequiredService<BottomNavViewModel>();
        await bar.OpenCommand.ExecuteAsync(Routes.Receive);
        Assert.Equal(Routes.Receive, navigation.RequestedRoutes.Last());

        var receive = await OpenAsync(services, OperationDirection.In);
        receive.EntryQuantity = "12";
        receive.QuickSearch.Query = "nut";
        var row = receive.QuickSearch.Suggestions[0];
        var before = row.Suggestion.Product.Quantity;
        receive.QuickSearch.PickCommand.Execute(row);
        await Eventually.WaitAsync(() => receive.Lines.Count == 1);
        await receive.CommitCommand.ExecuteAsync(null);

        var list = services.GetRequiredService<ProductListViewModel>();
        await list.EnsureLoadedCommand.ExecuteAsync(null);
        Assert.Equal(before + 12, list.Products.Single(p => p.Id == row.Suggestion.Product.Id).Quantity);
    }

    // entities.md "view own": what others recorded never reaches the Driver's Home
    [Fact]
    public async Task DriverReceivesTwice_AndHomeShowsOnlyHisRows()
    {
        using var services = TestContainer.Create(new FakePreferencesStore(), new FakeNavigationService());
        await SignInAsync(services, Role.Driver);
        var barcode = (await services.GetRequiredService<IProductCatalog>().GetSummariesAsync())[0].Barcode;
        for (var i = 0; i < 2; i++)
        {
            var draft = new StockDocumentDraft(OperationDirection.In);
            draft.Add(barcode, 4);
            Assert.True((await services.GetRequiredService<IStockDocumentService>().CommitAsync(draft)).IsSuccess);
        }

        var home = services.GetRequiredService<HomeViewModel>();
        await home.LoadCommand.ExecuteAsync(null);
        var activity = services.GetRequiredService<ActivityViewModel>();
        await activity.LoadCommand.ExecuteAsync(null);

        Assert.Equal([4, 4], Assert.Single(home.Today).Rows.Select(r => r.Entry.Quantity));
        var driver = services.GetRequiredService<ICurrentUserService>().CurrentUser!.Id;
        Assert.All(activity.Days.SelectMany(d => d.Rows), row => Assert.Equal(driver, row.Entry.UserId));
    }

    [Fact]
    public async Task DriverOpeningIssue_IsRefusedAndNothingIsWritten()
    {
        var navigation = new FakeNavigationService();
        using var services = TestContainer.Create(new FakePreferencesStore(), navigation);
        await SignInAsync(services, Role.Driver);
        var before = (await services.GetRequiredService<IOperationRepository>().GetAllAsync()).Count;
        var issue = services.GetRequiredService<StockDocumentViewModel>();
        issue.Direction = OperationDirection.Out;

        await issue.AppearCommand.ExecuteAsync(null);

        Assert.Equal(Routes.Home, navigation.RequestedRoutes.Last());
        Assert.Equal(before, (await services.GetRequiredService<IOperationRepository>().GetAllAsync()).Count);
    }

    [Fact]
    public async Task DraftIsRestoredAfterTheAppIsKilled()
    {
        var preferences = new FakePreferencesStore();
        var secrets = new FakeSecretStore();
        string barcode;
        using (var first = TestContainer.Create(preferences, new FakeNavigationService(), secrets: secrets))
        {
            await SignInAsync(first, Role.Manager);
            barcode = (await first.GetRequiredService<IProductCatalog>().GetSummariesAsync())[0].Barcode;
            var receive = await OpenAsync(first, OperationDirection.In);
            await TypeAsync(receive, barcode);
            first.GetRequiredService<IAppStateService>().Save();
        }

        using var second = TestContainer.Create(preferences, new FakeNavigationService(), secrets: secrets);
        second.GetRequiredService<IAppStateService>().Restore();
        await SignInAsync(second, Role.Manager);

        var restored = await OpenAsync(second, OperationDirection.In);

        Assert.Equal(barcode, Assert.Single(restored.Lines).Barcode);
    }
}
