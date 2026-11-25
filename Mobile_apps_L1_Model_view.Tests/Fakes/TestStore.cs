using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// In-memory repositories with the real services built on them, wired the same way as the
// container. ViewModel tests share one store, so a change made by one service is seen by the others.
public sealed class TestStore
{
    public TestStore()
    {
        AppState = new AppStateService(Preferences);
        Session = new SessionStore(Secrets, new HmacTokenService(Secrets, Clock), Users, Logger<SessionStore>());
        Drafts = new OperationDraftStore(AppState, new AesFieldEncryptor(Secrets), NullLogger<OperationDraftStore>.Instance);
        DeepLinks = new DeepLinkHandler(Catalog, CurrentUser, Navigation, Alerts);
        Startup = new StartupNavigator(AppState, DeepLinks, Catalog, Navigation);
    }

    // Everything the services and ViewModels built on this store log, in order
    public List<LogRecord> Logs { get; } = [];

    public FakeClock Clock { get; } = new();
    public FakeCrashReporter CrashReporter { get; } = new();
    public CurrentUserService CurrentUser { get; } = new();
    public FakeNavigationService Navigation { get; } = new();
    public FakePreferencesStore Preferences { get; } = new();
    public AppStateService AppState { get; }
    public FakeSecretStore Secrets { get; } = new();
    public SessionStore Session { get; }
    public OperationDraftStore Drafts { get; }
    public FakeAlertService Alerts { get; } = new();
    public DeepLinkHandler DeepLinks { get; }
    public StartupNavigator Startup { get; }

    // Device capabilities (Lab 7)
    public FakePhotoService Photos { get; } = new();
    public FakeLocationService Location { get; } = new();
    public FakeMapLauncher Maps { get; } = new();
    public FakeShakeDetector ShakeDetector { get; } = new();
    public FakeShakeSettings ShakeSettings { get; } = new();
    public FakeMainThread MainThread { get; } = new();
    public FakeSyncService Sync { get; } = new();
    public OfflineSimulator Offline { get; } = new();
    public FakeConnectivityMonitor Connectivity { get; } = new();

    public InMemoryUserRepository Users { get; } = new();
    public InMemoryProductRepository Products { get; } = new();
    public InMemoryCharacteristicRepository Characteristics { get; } = new();
    public InMemoryProductCharacteristicRepository Values { get; } = new();
    public InMemoryOperationRepository Operations { get; } = new();

    public ILogger<T> Logger<T>() => new CapturingLogger<T>(Logs);

    // The services are stateless wrappers over the repositories above, so building them on access is cheap
    public IStockService Stock => new StockService(Operations, Products, CurrentUser, Clock, Logger<StockService>());

    public IProductService ProductService =>
        new ProductService(Products, Operations, new ProductValidator(Products), CurrentUser, Clock, Logger<ProductService>());

    public ICharacteristicValidator Validator => new CharacteristicValidator(Characteristics);

    public ICharacteristicService CharacteristicService =>
        new CharacteristicService(Characteristics, Values, Products, Validator, CurrentUser, Clock,
            Logger<CharacteristicService>());

    public IDeviceFeedback Feedback => new DeviceFeedback(Logger<DeviceFeedback>());

    public ProductPhotoEditor PhotoEditor => new(Photos, Feedback, Guard, Logger<ProductPhotoEditor>());

    public ConnectivityBannerViewModel Banner => new(Sync, Offline, Connectivity, Logger<ConnectivityBannerViewModel>());

    public IShakeGesture ShakeGesture =>
        new ShakeGesture(ShakeDetector, ShakeSettings, CurrentUser, MainThread, Clock, Logger<ShakeGesture>());

    public IPermissionGuard Guard => new PermissionGuard(CurrentUser, Alerts, Logger<PermissionGuard>());

    public FakeBarcodeScanner Scanner { get; } = new();

    public FakeProductLookup Lookup { get; } = new();

    public QuickSearchViewModel QuickSearch =>
        new(Catalog, CharacteristicService, CurrentUser, Guard, Navigation, Logger<QuickSearchViewModel>());

    public ProductFormViewModel ProductForm => new(new ProductValidator(Products), ProductService, CurrentUser, Guard,
        AppState, Navigation, PhotoEditor, Scanner, Lookup, QuickSearch, Logger<ProductFormViewModel>());

    public IDiagnosticsService Diagnostics => new DiagnosticsService(CurrentUser, CrashReporter, Logger<DiagnosticsService>());

    public IProductCatalog Catalog => new ProductCatalog(Products, Characteristics, Values, Operations, Users, Stock);

    // Adds the user to the user repository (for history names) and signs them in
    public async Task<User> SignInAsync(Role role, string fullName = "Test User")
    {
        var user = new User { FullName = fullName, Role = role };
        await Users.AddAsync(user);
        CurrentUser.SignIn(user);
        return user;
    }

    public async Task<Product> AddProductAsync(string name, string barcode)
    {
        var product = new Product { Name = name, Barcode = barcode };
        await Products.AddAsync(product);
        return product;
    }

    public async Task<Characteristic> AddCharacteristicAsync(string name, CharacteristicValueType valueType)
    {
        var characteristic = new Characteristic { Name = name, ValueType = valueType };
        await Characteristics.AddAsync(characteristic);
        return characteristic;
    }

    public Task SetValueAsync(Product product, Characteristic characteristic, string value) =>
        Values.AddAsync(new ProductCharacteristic
        {
            ProductId = product.Id,
            CharacteristicId = characteristic.Id,
            Value = value
        });
}
