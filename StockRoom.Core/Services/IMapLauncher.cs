using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Shows a place in the maps app of the device (the replacement for an embedded map where there is none)
public interface IMapLauncher
{
    // Succeeded, NotSupported (no maps app) or Failed. Never throws
    Task<DeviceStatus> OpenAsync(GeoPoint location, string label);
}
