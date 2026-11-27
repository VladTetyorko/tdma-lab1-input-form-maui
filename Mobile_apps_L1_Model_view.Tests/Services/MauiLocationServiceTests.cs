using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class MauiLocationServiceTests
{
    private readonly FakeGeolocation _geolocation = new();
    private readonly FakePermissionService _permissions = new();
    private readonly MauiLocationService _service;

    public MauiLocationServiceTests()
    {
        _service = new MauiLocationService(_geolocation, _permissions);
    }

    [Fact]
    public async Task FreshFix_IsReturned_WithMediumAccuracyAndTenSecondTimeout()
    {
        var result = await _service.GetCurrentAsync();

        Assert.Equal(DeviceStatus.Succeeded, result.Status);
        Assert.Equal(new GeoPoint(50.4501, 30.5234), result.Point);
        Assert.Equal(GeolocationAccuracy.Medium, _geolocation.LastRequest!.DesiredAccuracy);
        Assert.Equal(TimeSpan.FromSeconds(10), _geolocation.LastRequest.Timeout);
        Assert.Equal([DeviceFeature.Location], _permissions.Asked);
        Assert.Equal(0, _geolocation.LastKnownCalls);
    }

    [Fact]
    public async Task NoFreshFix_FallsBackToTheLastKnownPosition()
    {
        _geolocation.Fresh = null;
        _geolocation.LastKnown = new Location(48.0, 24.0);

        var result = await _service.GetCurrentAsync();

        Assert.Equal(new GeoPoint(48.0, 24.0), result.Point);
        Assert.Equal(1, _geolocation.LastKnownCalls);
    }

    [Fact]
    public async Task NoFixAtAll_IsFailed()
    {
        _geolocation.Fresh = null;
        _geolocation.LastKnown = null;

        var result = await _service.GetCurrentAsync();

        Assert.Equal(DeviceStatus.Failed, result.Status);
        Assert.Null(result.Point);
    }

    [Fact]
    public async Task PermissionDenied_NeverAsksTheSensor()
    {
        _permissions.Results[DeviceFeature.Location] = PermissionResult.Denied;

        var result = await _service.GetCurrentAsync();

        Assert.Equal(DeviceStatus.PermissionDenied, result.Status);
        Assert.Equal(0, _geolocation.FreshCalls);
    }

    [Fact]
    public async Task PermissionUnavailable_IsNotSupported()
    {
        _permissions.Results[DeviceFeature.Location] = PermissionResult.Unavailable;

        Assert.Equal(DeviceStatus.NotSupported, (await _service.GetCurrentAsync()).Status);
    }

    [Theory]
    [MemberData(nameof(Throwables))]
    public async Task SensorExceptions_BecomeStatuses(Exception thrown, DeviceStatus expected)
    {
        _geolocation.FreshThrows = thrown;

        var result = await _service.GetCurrentAsync();

        Assert.Equal(expected, result.Status);
        if (expected == DeviceStatus.Failed)
        {
            Assert.Same(thrown, result.Error);
        }
    }

    public static TheoryData<Exception, DeviceStatus> Throwables() => new()
    {
        { new FeatureNotSupportedException(), DeviceStatus.NotSupported },
        { new FeatureNotEnabledException(), DeviceStatus.NotEnabled },
        { new PermissionException("location"), DeviceStatus.PermissionDenied },
        { new InvalidOperationException("boom"), DeviceStatus.Failed }
    };
}
