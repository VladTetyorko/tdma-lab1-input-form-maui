using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class MauiMapLauncher(IMap map, ILogger<MauiMapLauncher> logger) : IMapLauncher
{
    public async Task<DeviceStatus> OpenAsync(GeoPoint location, string label)
    {
        try
        {
            await map.OpenAsync(new Location(location.Latitude, location.Longitude),
                new MapLaunchOptions { Name = label });
            return DeviceStatus.Succeeded;
        }
        catch (FeatureNotSupportedException)
        {
            return DeviceStatus.NotSupported;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The caller reports it (IDeviceFeedback); only the detail is logged here
            logger.LogDebug(ex, "The maps app could not be opened");
            return DeviceStatus.Failed;
        }
    }
}
