using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// The platform services AddStockRoomCore expects from its host, as fakes: a temporary database file,
// an in-memory secret store, controllable connectivity, a recording file sharer and a fake network
// (no real HTTP request leaves a test), and the Lab 7 device capabilities (permissions, photos, location,
// shake sensor, maps, UI thread).
// The container owns (and disposes) the temp path, after the database has been closed.
public static class TestPlatform
{
    public static IServiceCollection AddFakePlatform(this IServiceCollection services)
    {
        services.AddSingleton<TempDatabasePath>();
        services.AddSingleton<IDatabasePathProvider>(provider => provider.GetRequiredService<TempDatabasePath>());
        services.AddSingleton<TempLogPath>();
        services.AddSingleton<ILogPathProvider>(provider => provider.GetRequiredService<TempLogPath>());
        services.AddSingleton<FakeCrashReporter>();
        services.AddSingleton<ICrashReporter>(provider => provider.GetRequiredService<FakeCrashReporter>());
        services.AddSingleton<FakeSecretStore>();
        services.AddSingleton<ISecretStore>(provider => provider.GetRequiredService<FakeSecretStore>());
        services.AddSingleton<FakeBiometricAuthenticator>();
        services.AddSingleton<IBiometricAuthenticator>(provider => provider.GetRequiredService<FakeBiometricAuthenticator>());
        services.AddSingleton<FakeConnectivityMonitor>();
        services.AddSingleton<IConnectivityMonitor>(provider => provider.GetRequiredService<FakeConnectivityMonitor>());
        services.AddSingleton<FakeFileSharer>();
        services.AddSingleton<IFileSharer>(provider => provider.GetRequiredService<FakeFileSharer>());
        services.AddSingleton<FakePlatformPermissions>();
        services.AddSingleton<IPlatformPermissions>(provider => provider.GetRequiredService<FakePlatformPermissions>());
        services.AddSingleton<FakePhotoService>();
        services.AddSingleton<IPhotoService>(provider => provider.GetRequiredService<FakePhotoService>());
        services.AddSingleton<FakeLocationService>();
        services.AddSingleton<ILocationService>(provider => provider.GetRequiredService<FakeLocationService>());
        services.AddSingleton<FakeShakeDetector>();
        services.AddSingleton<IShakeDetector>(provider => provider.GetRequiredService<FakeShakeDetector>());
        services.AddSingleton<FakeMapLauncher>();
        services.AddSingleton<IMapLauncher>(provider => provider.GetRequiredService<FakeMapLauncher>());
        services.AddSingleton<FakeMainThread>();
        services.AddSingleton<IMainThread>(provider => provider.GetRequiredService<FakeMainThread>());
        services.AddSingleton<FakeHttpMessageHandler>();
        services.AddSingleton<HttpMessageHandler>(provider => provider.GetRequiredService<FakeHttpMessageHandler>());
        return services;
    }
}
