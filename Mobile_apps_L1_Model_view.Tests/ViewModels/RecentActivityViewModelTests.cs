using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class RecentActivityViewModelTests
{
    private readonly TestStore _store = new();
    private readonly InMemoryStockDocumentRepository _documents;
    private readonly RecentActivityViewModel _viewModel;

    public RecentActivityViewModelTests()
    {
        _documents = new InMemoryStockDocumentRepository(_store.Operations);
        _viewModel = new RecentActivityViewModel(_store.Operations, _store.Products, _store.Users, _documents,
            _store.CurrentUser, new CapturingLogger<RecentActivityViewModel>());
    }

    private Task AddAsync(Guid product, Guid user, int quantity, int minute) => _store.Operations.AddAsync(new Operation
    {
        ProductId = product,
        CreatedByUserId = user,
        Quantity = quantity,
        Direction = quantity > 2 ? OperationDirection.In : OperationDirection.Out,
        CreatedAt = new DateTime(2026, 11, 24, 10, minute, 0, DateTimeKind.Utc)
    });

    [Fact]
    public async Task Refresh_ShowsTheLastFive_NewestFirst()
    {
        var user = await _store.SignInAsync(Role.Manager, "Olena");
        var drill = await _store.AddProductAsync("Drill", "4006381333931");
        for (var i = 1; i <= 7; i++)
        {
            await AddAsync(drill.Id, user.Id, i, i);
        }

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(RecentActivityViewModel.Count, _viewModel.Items.Count);
        Assert.Equal("+7 Drill", _viewModel.Items[0].Summary);
        Assert.True(_viewModel.HasItems);
    }

    [Fact]
    public async Task Refresh_ForADriver_ShowsEveryonesOperations()
    {
        var driver = await _store.SignInAsync(Role.Driver, "Petro");
        var drill = await _store.AddProductAsync("Drill", "4006381333931");
        await AddAsync(drill.Id, Guid.NewGuid(), 5, 1);
        await AddAsync(drill.Id, driver.Id, 3, 2);

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(["+3 Drill", "+5 Drill"], _viewModel.Items.Select(item => item.Summary));
    }

    [Fact]
    public async Task Refresh_NobodySignedIn_IsEmpty()
    {
        await _viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.False(_viewModel.HasItems);
    }
}
