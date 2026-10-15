using Microsoft.Extensions.DependencyInjection;
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
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IAppStateService, AppStateService>();
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

        services.AddTransient<LoginViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<ProductFormViewModel>();
        services.AddTransient<ProductDetailViewModel>();
        services.AddTransient<CharacteristicsViewModel>();
        services.AddTransient<OperationViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ScannerViewModel>();

        return services;
    }
}
