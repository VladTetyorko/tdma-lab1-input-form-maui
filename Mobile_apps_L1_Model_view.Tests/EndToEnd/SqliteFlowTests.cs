using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;
using SQLite;

namespace Mobile_apps_L1_Model_view.Tests.EndToEnd;

// Whole flows through the real container on a real SQLite file. Starting the app again reuses the same
// file, secrets and preferences, which is what a restart of the device does
public class SqliteFlowTests : IDisposable
{
    private const string DrillBarcode = "482000100001";

    private readonly TempDatabasePath _files = new();
    private readonly FakeSecretStore _secrets = new();
    private readonly FakePreferencesStore _preferences = new();
    private readonly FakeNavigationService _navigation = new();
    private readonly FakeAlertService _alerts = new();
    private readonly FakeConnectivityMonitor _network = new();
    private readonly FakeFileSharer _sharer = new();
    private ServiceProvider? _app;

    public void Dispose()
    {
        _app?.Dispose();
        _files.Dispose();
    }

    private ServiceProvider App => _app ?? throw new InvalidOperationException("The app is not running.");

    // Starts the app on the same database file (the first start creates and seeds it)
    private ServiceProvider Start()
    {
        var services = new ServiceCollection().AddStockRoomCore();
        services.AddSingleton<IDatabasePathProvider>(_files);
        services.AddSingleton<ISecretStore>(_secrets);
        services.AddSingleton<IConnectivityMonitor>(_network);
        services.AddSingleton<IFileSharer>(_sharer);
        services.AddSingleton<IPreferencesStore>(_preferences);
        services.AddSingleton<INavigationService>(_navigation);
        services.AddSingleton<IAlertService>(_alerts);
        services.AddSingleton<IThemeApplier>(new FakeThemeApplier());
        services.AddSingleton<IBarcodeScanner>(new FakeBarcodeScanner());
        _app = services.BuildServiceProvider();
        return _app;
    }

    // Closes the app like the OS does: the window is stopped (state saved), then the process ends
    private void Stop()
    {
        App.GetRequiredService<IAppLifecycleService>().OnStopped();
        _app!.Dispose();
        _app = null;
    }

    private async Task SignInAsync(Role role)
    {
        var login = App.GetRequiredService<LoginViewModel>();
        await login.LoadCommand.ExecuteAsync(null);
        login.SelectedUser = login.Users.Single(u => u.Role == role);
        await login.SignInCommand.ExecuteAsync(null);
    }

    private async Task<Product> DrillAsync() =>
        (await App.GetRequiredService<IProductRepository>().GetByBarcodeAsync(Ean13.WithCheckDigit(DrillBarcode)))!;

    [Fact]
    public async Task OfflineChange_IsQueuedAndQuantityDropsLocally_ThenSentWhenOnlineAgain()
    {
        var app = Start();
        var sync = app.GetRequiredService<ISyncService>();
        var server = app.GetRequiredService<StubSyncApi>();
        var queue = app.GetRequiredService<ISyncQueue>();
        await app.GetRequiredService<IAppLifecycleService>().RestoreAsync();
        var settings = app.GetRequiredService<SettingsViewModel>();

        settings.SimulateOffline = true;
        Assert.False(sync.IsOnline);
        await SignInAsync(Role.Storekeeper);
        var drill = await DrillAsync();

        var recorded = await app.GetRequiredService<IStockService>()
            .RecordAsync(drill.Id, OperationDirection.Out, 2, counterparty: "Acme Ltd");
        Assert.True(recorded.IsSuccess, recorded.Error);

        var list = app.GetRequiredService<ProductListViewModel>();
        await list.LoadCommand.ExecuteAsync(null);
        Assert.Equal(19, Assert.Single(list.Products, p => p.Id == drill.Id).Quantity);
        Assert.Equal(1, await sync.GetPendingCountAsync());
        var queued = Assert.Single(await queue.GetPendingAsync());
        Assert.Equal(("Operations", SyncOperation.Insert), (queued.EntityType, queued.Operation));
        Assert.Empty(server.Records);

        var synced = new TaskCompletionSource<SyncResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        sync.Completed += (_, result) =>
        {
            if (result.Status == SyncStatus.Synced && result.Remaining == 0)
            {
                synced.TrySetResult(result);
            }
        };

        settings.SimulateOffline = false;

        var result = await synced.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(1, result.Pushed);
        Assert.Empty(await queue.GetPendingAsync());
        var record = Assert.Single(server.Records);
        Assert.Equal("Operations", record.EntityType);
        Assert.Contains("\"Quantity\":2", record.PayloadJson);
        Assert.DoesNotContain("Acme", record.PayloadJson);
    }

    [Fact]
    public async Task ProductAddedBeforeARestart_IsStillThere_AndNothingIsSeededTwice()
    {
        Start();
        await SignInAsync(Role.Manager);
        var form = App.GetRequiredService<ProductFormViewModel>();
        form.Name = "Persisted Drill";
        form.Barcode = Ean13.WithCheckDigit("400638133394");
        await form.SaveCommand.ExecuteAsync(null);
        Assert.Equal(Routes.Back, _navigation.RequestedRoutes.Last());
        Stop();
        Assert.True(File.Exists(_files.DatabasePath));

        var restarted = Start();
        var route = await restarted.GetRequiredService<IAppLifecycleService>().RestoreAsync();

        Assert.Equal(Routes.Products, route);
        var list = restarted.GetRequiredService<ProductListViewModel>();
        await list.LoadCommand.ExecuteAsync(null);
        Assert.Equal(11, list.Products.Count);
        Assert.Single(list.Products, p => p.Name == "Persisted Drill");
        Assert.Equal(3, (await restarted.GetRequiredService<IUserRepository>().GetAllAsync()).Count);
        Assert.Equal(4, (await restarted.GetRequiredService<ICharacteristicRepository>().GetAllAsync()).Count);
    }

    [Fact]
    public async Task Manager_EditsABarcode_DeletesAProductWithoutHistory_AndIsRefusedForOneWithHistory()
    {
        var app = Start();
        await SignInAsync(Role.Manager);
        var products = app.GetRequiredService<IProductRepository>();
        var list = app.GetRequiredService<ProductListViewModel>();

        var form = app.GetRequiredService<ProductFormViewModel>();
        form.Name = "Spare Saw";
        form.Barcode = Ean13.WithCheckDigit("400638133394");
        await form.SaveCommand.ExecuteAsync(null);
        await list.LoadCommand.ExecuteAsync(null);
        var saw = Assert.Single(list.Products, p => p.Name == "Spare Saw");

        // Edit: the form reopens with the product and the new barcode is saved
        await list.EditProductCommand.ExecuteAsync(saw);
        var editForm = app.GetRequiredService<ProductFormViewModel>();
        editForm.ApplyQueryAttributes(_navigation.RequestedParameters.Last()!);
        Assert.Equal("Edit product", editForm.PageTitle);
        editForm.Barcode = Ean13.WithCheckDigit("400638133401");
        await editForm.SaveCommand.ExecuteAsync(null);
        list.ApplyQueryAttributes(_navigation.RequestedParameters.Last()!);

        Assert.Equal(Ean13.WithCheckDigit("400638133401"), Assert.Single(list.Products, p => p.Id == saw.Id).Barcode);
        Assert.Null(await products.GetByBarcodeAsync(Ean13.WithCheckDigit("400638133394")));
        Assert.NotNull(await products.GetByBarcodeAsync(Ean13.WithCheckDigit("400638133401")));

        // Delete: a product without operations goes after confirmation
        await list.DeleteProductCommand.ExecuteAsync(Assert.Single(list.Products, p => p.Id == saw.Id));
        Assert.DoesNotContain(list.Products, p => p.Id == saw.Id);
        Assert.Null(await products.GetByIdAsync(saw.Id));
        Assert.Single(_alerts.Confirmations);

        // A product with history is refused, with the reason, and nothing is asked
        var drill = Assert.Single(list.Products, p => p.Name == "Cordless Drill 18V");
        Assert.False(drill.CanDelete);
        await list.DeleteProductCommand.ExecuteAsync(drill);

        Assert.Equal(ProductService.HasOperationsMessage, list.StatusMessage);
        Assert.Contains(list.Products, p => p.Id == drill.Id);
        Assert.NotNull(await products.GetByIdAsync(drill.Id));
        Assert.Single(_alerts.Confirmations);
    }

    [Fact]
    public async Task Counterparty_IsEncryptedInTheFile_ButShownInTheProductHistory()
    {
        var app = Start();
        await SignInAsync(Role.Driver);
        var drill = await DrillAsync();

        var recorded = await app.GetRequiredService<IStockService>()
            .RecordAsync(drill.Id, OperationDirection.In, 5, counterparty: "Acme Ltd");
        Assert.True(recorded.IsSuccess, recorded.Error);
        var operationId = recorded.Value!.Id.ToString();

        var stored = await (await app.GetRequiredService<IDatabase>().GetConnectionAsync())
            .ExecuteScalarAsync<string>("SELECT Counterparty FROM Operations WHERE Id = ?", operationId);
        Assert.NotNull(stored);
        Assert.NotEqual("Acme Ltd", stored);
        Assert.DoesNotContain("Acme", stored);

        // The exported file (what leaves the device) and the outbox hold no plain text either
        var copy = await app.GetRequiredService<IDatabaseExporter>().ShareAsync();
        Assert.DoesNotContain("Acme", Encoding.UTF8.GetString(await File.ReadAllBytesAsync(copy)));
        var pending = await app.GetRequiredService<ISyncQueue>().GetPendingAsync();
        Assert.All(pending, item => Assert.DoesNotContain("Acme", item.PayloadJson));

        // The app reads it back for the history
        var summary = (await app.GetRequiredService<IProductCatalog>().GetSummaryAsync(drill.Id))!;
        var detail = app.GetRequiredService<ProductDetailViewModel>();
        detail.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = summary });
        await detail.LoadCommand.ExecuteAsync(null);

        Assert.Contains(detail.History, h => h.Counterparty == "Acme Ltd");
    }
}
