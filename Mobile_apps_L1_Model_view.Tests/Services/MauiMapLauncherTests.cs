using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class MauiMapLauncherTests
{
    private readonly FakeMap _map = new();
    private readonly MauiMapLauncher _launcher;

    public MauiMapLauncherTests()
    {
        _launcher = new MauiMapLauncher(_map, NullLogger<MauiMapLauncher>.Instance);
    }

    [Fact]
    public async Task Open_PassesThePlaceAndTheLabelToTheMapsApp()
    {
        var status = await _launcher.OpenAsync(new GeoPoint(50.45, 30.52), "In 5 x Saw");

        Assert.Equal(DeviceStatus.Succeeded, status);
        Assert.Equal((50.45, 30.52, "In 5 x Saw"), Assert.Single(_map.Opened));
    }

    [Fact]
    public async Task Open_NoMapsApp_IsNotSupported()
    {
        _map.Throws = new FeatureNotSupportedException();

        Assert.Equal(DeviceStatus.NotSupported, await _launcher.OpenAsync(new GeoPoint(1, 2), "x"));
    }

    [Fact]
    public async Task Open_AnythingElse_IsFailed()
    {
        _map.Throws = new InvalidOperationException("boom");

        Assert.Equal(DeviceStatus.Failed, await _launcher.OpenAsync(new GeoPoint(1, 2), "x"));
    }
}
