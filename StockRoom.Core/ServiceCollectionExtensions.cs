using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Data.Migrations;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view;

public static class ServiceCollectionExtensions
{
    // Everything except pages and platform services (INavigationService, IPreferencesStore, ...),
    // so tests can build the same container with fake platform services
    public static IServiceCollection AddStockRoomCore(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<UnhandledExceptionLogger>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();

        // Local accounts: PBKDF2 hashes, login with lockout, signed session token kept in the secret store
        services.AddSingleton<IPasswordHasher>(_ => new Pbkdf2PasswordHasher());
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ITokenService, HmacTokenService>();
        services.AddSingleton<ISessionStore, SessionStore>();
        services.AddSingleton<IBiometricSettings, BiometricSettings>();
        services.AddSingleton<IPermissionGuard, PermissionGuard>();

        // Device capabilities (Lab 7): the permission flow, the user messages and the shake gesture are platform-free;
        // IPlatformPermissions, IPhotoService, ILocationService, IShakeDetector, IMapLauncher and IMainThread come from the host
        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddSingleton<IDeviceFeedback, DeviceFeedback>();
        services.AddSingleton<PhotoStore>();
        services.AddSingleton<IShakeSettings, ShakeSettings>();
        services.AddTransient<IShakeGesture, ShakeGesture>();
        services.AddSingleton<IAppStateService, AppStateService>();
        services.AddSingleton<IOperationDraftStore, OperationDraftStore>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IDeepLinkHandler, DeepLinkHandler>();
        services.AddSingleton<IStartupNavigator, StartupNavigator>();
        services.AddSingleton<IAppLifecycleService, AppLifecycleService>();

        // Storage: one SQLite database, opened and migrated on first use. The file path, secret store,
        // connectivity and file sharing are platform services registered by the host (MauiProgram or tests)
        services.AddSingleton<IMigrationSource, EmbeddedMigrationSource>();
        services.AddSingleton<MigrationRunner>();
        services.AddSingleton<IDatabase, SqliteDatabase>();
        services.AddSingleton<SyncJournal>();
        services.AddSingleton<ISyncQueue, SqliteSyncQueue>();
        services.AddSingleton<IUserRepository, SqliteUserRepository>();
        services.AddSingleton<IProductRepository, SqliteProductRepository>();
        services.AddSingleton<ICharacteristicRepository, SqliteCharacteristicRepository>();
        services.AddSingleton<IProductCharacteristicRepository, SqliteProductCharacteristicRepository>();
        services.AddSingleton<IOperationRepository, SqliteOperationRepository>();
        services.AddSingleton<IStockDocumentRepository, SqliteStockDocumentRepository>();
        services.AddSingleton<IFieldEncryptor, AesFieldEncryptor>();

        services.AddSingleton<StubSyncApi>();
        services.AddSingleton<ISyncApi>(provider => provider.GetRequiredService<StubSyncApi>());
        services.AddSingleton<IOfflineSimulator, OfflineSimulator>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddSingleton<IDatabaseExporter, DatabaseExporter>();

        services.AddSingleton<IProductValidator, ProductValidator>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<ICharacteristicValidator, CharacteristicValidator>();
        services.AddSingleton<ICharacteristicService, CharacteristicService>();
        services.AddSingleton<IStockService, StockService>();
        services.AddSingleton<IProductCatalog, ProductCatalog>();
        services.AddSingleton<StockChanges>();
        services.AddSingleton<IStockDocumentDraftStore, StockDocumentDraftStore>();
        services.AddSingleton<IStockDocumentService, StockDocumentService>();

        // Logging tools: the log folder is a platform service (ILogPathProvider), the crash reporter is chosen by the host
        services.AddSingleton<ILogReader, LogReader>();
        services.AddSingleton<IDiagnosticsService, DiagnosticsService>();

        // Open product data: one HttpClient (5 s timeout, generic User-Agent) over a replaceable handler,
        // so tests register a fake HttpMessageHandler instead of touching the network
        services.AddSingleton<HttpMessageHandler>(_ => new HttpClientHandler());
        services.AddSingleton<IProductLookup>(provider => new OpenFoodFactsLookup(
            OpenFoodFactsLookup.CreateClient(provider.GetRequiredService<HttpMessageHandler>()),
            provider.GetRequiredService<ILogger<OpenFoodFactsLookup>>()));

        services.AddTransient<LoginViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<ProductPhotoEditor>();
        services.AddTransient<ConnectivityBannerViewModel>();
        services.AddSingleton<SessionStatusViewModel>();
        services.AddSingleton<DrawerViewModel>();
        services.AddTransient<ProductFormViewModel>();
        services.AddTransient<ProductDetailViewModel>();
        services.AddTransient<CharacteristicsViewModel>();
        services.AddTransient<OperationViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ScannerViewModel>();
        services.AddTransient<LogsViewModel>();
        services.AddTransient<StockDocumentViewModel>();
        services.AddTransient<RecentActivityViewModel>();
        services.AddTransient<QuickSearchViewModel>();
        services.AddTransient<MapViewModel>();
        services.AddSingleton<AppBarViewModel>();

        return services;
    }
}
