using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// The position over IGeolocation: permission first, then a fresh Medium-accuracy fix (10 seconds), then the last known
public sealed class MauiLocationService(IGeolocation geolocation, IPermissionService permissions) : ILocationService
{
    public static readonly TimeSpan FreshFixTimeout = TimeSpan.FromSeconds(10);

    public async Task<LocationResult> GetCurrentAsync()
    {
        var permission = await permissions.EnsureAsync(DeviceFeature.Location);
        if (permission != PermissionResult.Granted)
        {
            return LocationResult.Of(permission == PermissionResult.Denied ? DeviceStatus.PermissionDenied : DeviceStatus.NotSupported);
        }

        try
        {
            var location = await geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, FreshFixTimeout))
                           ?? await geolocation.GetLastKnownLocationAsync();

            // Nothing (a PC without a location sensor, no fix in time and nothing remembered) is a failure to explain
            var point = location is null ? null : GeoPoint.TryCreate(location.Latitude, location.Longitude);
            return point is null ? LocationResult.Of(DeviceStatus.Failed) : LocationResult.Found(point);
        }
        catch (FeatureNotSupportedException)
        {
            return LocationResult.Of(DeviceStatus.NotSupported);
        }
        catch (FeatureNotEnabledException)
        {
            return LocationResult.Of(DeviceStatus.NotEnabled);
        }
        catch (PermissionException)
        {
            return LocationResult.Of(DeviceStatus.PermissionDenied);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return LocationResult.Failure(ex);
        }
    }
}
