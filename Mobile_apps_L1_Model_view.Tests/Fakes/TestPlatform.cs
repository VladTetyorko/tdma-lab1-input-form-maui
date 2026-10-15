using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// The platform services AddStockRoomCore expects from its host, as fakes: a temporary database file,
// an in-memory secret store, controllable connectivity and a recording file sharer.
// The container owns (and disposes) the temp path, after the database has been closed.
public static class TestPlatform
{
    public static IServiceCollection AddFakePlatform(this IServiceCollection services)
    {
        services.AddSingleton<TempDatabasePath>();
        services.AddSingleton<IDatabasePathProvider>(provider => provider.GetRequiredService<TempDatabasePath>());
        services.AddSingleton<FakeSecretStore>();
        services.AddSingleton<ISecretStore>(provider => provider.GetRequiredService<FakeSecretStore>());
        services.AddSingleton<FakeConnectivityMonitor>();
        services.AddSingleton<IConnectivityMonitor>(provider => provider.GetRequiredService<FakeConnectivityMonitor>());
        services.AddSingleton<FakeFileSharer>();
        services.AddSingleton<IFileSharer>(provider => provider.GetRequiredService<FakeFileSharer>());
        return services;
    }
}
