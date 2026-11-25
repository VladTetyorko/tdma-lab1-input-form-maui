using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Data.Migrations;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Mobile_apps_L1_Model_view.Tests.Data;

// Against a real temporary SQLite file, built by the real container
public class StockDocumentRepositoryTests : IDisposable
{
    private readonly ServiceProvider _services = TestContainer.Create(new FakePreferencesStore(), new FakeNavigationService());

    private T Get<T>() where T : notnull => _services.GetRequiredService<T>();

    public void Dispose() => _services.Dispose();

    private async Task<(Guid User, IReadOnlyList<Product> Products)> SeedAsync()
    {
        var user = (await Get<IUserRepository>().GetAllAsync())[0];
        var products = await Get<IProductRepository>().GetAllAsync();
        return (user.Id, products);
    }

    private static (StockDocument, List<Operation>) Build(Guid user, IEnumerable<Guid> productIds, string number, DateTime at)
    {
        var document = new StockDocument
        {
            Number = number,
            Direction = OperationDirection.In,
            Counterparty = "Acme Foods",
            Note = "waybill 7",
            CreatedByUserId = user,
            CreatedAt = at,
            UpdatedAt = at
        };
        var lines = productIds.Select(id => new Operation
        {
            ProductId = id,
            DocumentId = document.Id,
            Direction = OperationDirection.In,
            Quantity = 3,
            CreatedByUserId = user,
            CreatedAt = at,
            UpdatedAt = at
        }).ToList();
        return (document, lines);
    }

    private static readonly DateTime Noon = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Add_SavesTheDocumentItsLinesAndTheQueueInOrder()
    {
        var (user, products) = await SeedAsync();
        var before = (await Get<ISyncQueue>().GetPendingAsync()).Count;
        var (document, lines) = Build(user, products.Take(3).Select(p => p.Id), "R-20261124-0001", Noon);

        await Get<IStockDocumentRepository>().AddAsync(document, lines);

        var saved = await Get<IStockDocumentRepository>().GetByIdAsync(document.Id);
        Assert.Equal(("R-20261124-0001", "Acme Foods", "waybill 7"), (saved!.Number, saved.Counterparty, saved.Note));
        var operations = (await Get<IOperationRepository>().GetAllAsync()).Where(o => o.DocumentId == document.Id).ToList();
        Assert.Equal(3, operations.Count);
        var queued = (await Get<ISyncQueue>().GetPendingAsync()).Skip(before).Select(item => item.EntityType);
        Assert.Equal(["StockDocuments", "Operations", "Operations", "Operations"], queued);
    }

    [Fact]
    public async Task Add_StoresTheCounterpartyEncrypted()
    {
        var (user, products) = await SeedAsync();
        var (document, lines) = Build(user, [products[0].Id], "R-20261124-0001", Noon);

        await Get<IStockDocumentRepository>().AddAsync(document, lines);

        var connection = await Get<IDatabase>().GetConnectionAsync();
        var stored = await connection.ExecuteScalarAsync<string>("SELECT Counterparty FROM StockDocuments");
        Assert.NotEqual("Acme Foods", stored);
        var payload = (await Get<ISyncQueue>().GetPendingAsync()).Single(i => i.EntityType == "StockDocuments").PayloadJson;
        Assert.DoesNotContain("Acme", payload);
    }

    [Fact]
    public async Task Add_OneBadLine_RollsEverythingBack()
    {
        var (user, products) = await SeedAsync();
        var before = (await Get<ISyncQueue>().GetPendingAsync()).Count;
        var operationsBefore = (await Get<IOperationRepository>().GetAllAsync()).Count;
        var (document, lines) = Build(user, [products[0].Id, Guid.NewGuid()], "R-20261124-0001", Noon);

        await Assert.ThrowsAnyAsync<Exception>(() => Get<IStockDocumentRepository>().AddAsync(document, lines));

        Assert.Null(await Get<IStockDocumentRepository>().GetByIdAsync(document.Id));
        Assert.Equal(operationsBefore, (await Get<IOperationRepository>().GetAllAsync()).Count);
        Assert.Equal(before, (await Get<ISyncQueue>().GetPendingAsync()).Count);
    }

    [Fact]
    public async Task Add_LineOfAnotherDocument_IsRefused()
    {
        var (user, products) = await SeedAsync();
        var (document, _) = Build(user, [products[0].Id], "R-20261124-0001", Noon);
        var (_, foreign) = Build(user, [products[0].Id], "R-20261124-0002", Noon);

        await Assert.ThrowsAsync<ArgumentException>(() => Get<IStockDocumentRepository>().AddAsync(document, foreign));
    }

    [Fact]
    public async Task Recent_NewestFirst_AndNumbersAreCountedByPrefix()
    {
        var (user, products) = await SeedAsync();
        var repository = Get<IStockDocumentRepository>();
        var (older, olderLines) = Build(user, [products[0].Id], "R-20261124-0001", Noon);
        var (newer, newerLines) = Build(user, [products[1].Id], "R-20261124-0002", Noon.AddMinutes(5));
        await repository.AddAsync(older, olderLines);
        await repository.AddAsync(newer, newerLines);

        Assert.Equal([newer.Id, older.Id], (await repository.GetRecentAsync(5)).Select(d => d.Id));
        Assert.Equal(2, await repository.CountByNumberPrefixAsync("R-20261124-"));
        Assert.Equal(0, await repository.CountByNumberPrefixAsync("I-20261124-"));
        var recent = await Get<IOperationRepository>().GetRecentAsync(1);
        Assert.Equal(newer.Id, Assert.Single(recent).DocumentId);
    }
}
