using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeLocationService : ILocationService
{
    public LocationResult Result { get; set; } = LocationResult.Found(new GeoPoint(50.4501, 30.5234));

    public int Calls { get; private set; }

    public Task<LocationResult> GetCurrentAsync()
    {
        Calls++;
        return Task.FromResult(Result);
    }
}
