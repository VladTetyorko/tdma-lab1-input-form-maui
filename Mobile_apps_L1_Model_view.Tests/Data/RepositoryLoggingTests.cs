using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Data;

// A repository write logs its result once: Debug when committed, Error (with the exception) when it fails
public class RepositoryLoggingTests : IDisposable
{
    private readonly ServiceProvider _provider = new ServiceCollection().AddStockRoomCore().AddFakePlatform()
        .BuildServiceProvider();

    private readonly List<LogRecord> _logs = [];

    public void Dispose() => _provider.Dispose();

    private IProductRepository CreateRepository() => new SqliteProductRepository(
        _provider.GetRequiredService<IDatabase>(), _provider.GetRequiredService<SyncJournal>(),
        new CapturingLogger<SqliteProductRepository>(_logs));

    [Fact]
    public async Task Add_LogsOneDebugLine()
    {
        await CreateRepository().AddAsync(new Product { Name = "Saw", Barcode = "4006381333931", CreatedByUserId = SeedData.ManagerId });

        var record = Assert.Single(_logs);
        Assert.Equal(LogLevel.Debug, record.Level);
        Assert.Contains("Products", record.Message);
    }

    [Fact]
    public async Task UpdateOfAMissingRow_LogsAnErrorWithTheException()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.UpdateAsync(new Product { Name = "Ghost", Barcode = "4006381333931" }));

        var record = Assert.Single(_logs);
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.IsType<InvalidOperationException>(record.Exception);
    }
}
