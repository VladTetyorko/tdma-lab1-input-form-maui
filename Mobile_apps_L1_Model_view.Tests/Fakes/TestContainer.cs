using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// The real DI container (AddStockRoomCore) with fake platform services and a fresh, seeded temporary SQLite database.
// Passing the same preferences (and secret store) to two containers simulates the app being killed and started again.
// Every fake is also resolvable by its concrete type (FakeAlertService, FakeThemeApplier, ...).
public static class TestContainer
{
    public static ServiceProvider Create(
        FakePreferencesStore preferences,
        FakeNavigationService navigation,
        FakeBarcodeScanner? scanner = null,
        FakeSecretStore? secrets = null)
    {
        var services = new ServiceCollection().AddStockRoomCore().AddFakePlatform();
        var alerts = new FakeAlertService();
        var applier = new FakeThemeApplier();

        services.AddSingleton(preferences);
        services.AddSingleton<IPreferencesStore>(preferences);
        services.AddSingleton(navigation);
        services.AddSingleton<INavigationService>(navigation);
        services.AddSingleton(alerts);
        services.AddSingleton<IAlertService>(alerts);
        services.AddSingleton<FakeNavigationDrawer>();
        services.AddSingleton<INavigationDrawer>(provider => provider.GetRequiredService<FakeNavigationDrawer>());
        services.AddSingleton(applier);
        services.AddSingleton<IThemeApplier>(applier);
        var feedback = new FakeScanFeedback();
        var toasts = new FakeToastService();
        services.AddSingleton(feedback);
        services.AddSingleton<IScanFeedback>(feedback);
        services.AddSingleton(toasts);
        services.AddSingleton<IToastService>(toasts);
        services.AddSingleton(scanner ?? new FakeBarcodeScanner());
        services.AddSingleton<IBarcodeScanner>(provider => provider.GetRequiredService<FakeBarcodeScanner>());

        if (secrets is not null)
        {
            // Same secret store in two containers = the same device keystore after a restart
            services.AddSingleton(secrets);
            services.AddSingleton<ISecretStore>(secrets);
        }

        // The database is created, migrated and seeded by the first query
        return services.BuildServiceProvider();
    }
}
