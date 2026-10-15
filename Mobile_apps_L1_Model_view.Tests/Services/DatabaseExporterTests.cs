using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using SQLite;

namespace Mobile_apps_L1_Model_view.Tests.Services;

// Share database: a complete copy of the file goes to the share sheet, never the live file
public class DatabaseExporterTests : IDisposable
{
    private readonly ServiceProvider _provider = new ServiceCollection().AddStockRoomCore().AddFakePlatform()
        .BuildServiceProvider();

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task ShareAsync_HandsTheCopyToTheSharer_UnderTheExportName()
    {
        var exporter = _provider.GetRequiredService<IDatabaseExporter>();

        var path = await exporter.ShareAsync();

        var shared = Assert.Single(_provider.GetRequiredService<FakeFileSharer>().Shared);
        Assert.Equal("StockRoom database", shared.Title);
        Assert.Equal(path, shared.FilePath);
        Assert.Equal(
            Path.Combine(_provider.GetRequiredService<TempDatabasePath>().ExportDirectory, DatabaseExporter.ExportFileName),
            path);
        Assert.True(File.Exists(path));
        Assert.NotEqual(_provider.GetRequiredService<TempDatabasePath>().DatabasePath, path);
    }

    [Fact]
    public async Task ShareAsync_TheCopyIsAReadableDatabase_WithTheChangesMadeSoFar()
    {
        var products = _provider.GetRequiredService<IProductRepository>();
        await products.AddAsync(new Product
        {
            Name = "Written just before the export", Barcode = "4006381333931", CreatedByUserId = SeedData.ManagerId
        });

        var path = await _provider.GetRequiredService<IDatabaseExporter>().ShareAsync();

        var copy = new SQLiteAsyncConnection(path, SQLiteOpenFlags.ReadOnly);
        try
        {
            Assert.Equal("ok", await copy.ExecuteScalarAsync<string>("PRAGMA integrity_check"));
            Assert.Equal(11, await copy.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products"));
            Assert.Equal(2, await copy.ExecuteScalarAsync<int>("PRAGMA user_version"));
        }
        finally
        {
            await copy.CloseAsync();
        }
    }

    [Fact]
    public async Task ShareAsync_CalledTwice_ReplacesTheCopy()
    {
        var exporter = _provider.GetRequiredService<IDatabaseExporter>();

        await exporter.ShareAsync();
        await exporter.ShareAsync();

        Assert.Equal(2, _provider.GetRequiredService<FakeFileSharer>().Shared.Count);
    }
}
