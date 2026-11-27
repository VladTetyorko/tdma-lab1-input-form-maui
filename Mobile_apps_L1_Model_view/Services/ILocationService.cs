namespace Mobile_apps_L1_Model_view.Services;

// The position of the device. Never throws: PermissionDenied, NotEnabled (GPS off), NotSupported and Failed are results
public interface ILocationService
{
    // Asks for the location permission, then tries a fresh Medium-accuracy fix for 10 seconds and falls back to
    // the last known position
    Task<LocationResult> GetCurrentAsync();
}
