using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// The real DI container (AddStockRoomCore) with fake platform services and seeded demo data.
// Passing the same preferences to two containers simulates the app being killed and started again.
// Every fake is also resolvable by its concrete type (FakeAlertService, FakeThemeApplier, ...).
public static class TestContainer
{
    public static ServiceProvider Create(
        FakePreferencesStore preferences,
        FakeNavigationService navigation,
        FakeBarcodeScanner? scanner = null)
    {
        var services = new ServiceCollection().AddStockRoomCore();
        var alerts = new FakeAlertService();
        var applier = new FakeThemeApplier();

        services.AddSingleton(preferences);
        services.AddSingleton<IPreferencesStore>(preferences);
        services.AddSingleton(navigation);
        services.AddSingleton<INavigationService>(navigation);
        services.AddSingleton(alerts);
        services.AddSingleton<IAlertService>(alerts);
        services.AddSingleton(applier);
        services.AddSingleton<IThemeApplier>(applier);
        services.AddSingleton(scanner ?? new FakeBarcodeScanner());
        services.AddSingleton<IBarcodeScanner>(provider => provider.GetRequiredService<FakeBarcodeScanner>());

        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IDataSeeder>().SeedAsync().GetAwaiter().GetResult();
        return provider;
    }
}
