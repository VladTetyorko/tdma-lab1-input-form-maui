using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeLocationService : ILocationService
{
    public LocationResult Result { get; set; } = LocationResult.Found(new GeoPoint(50.4501, 30.5234));

    public int Calls { get; private set; }

    // Set to keep the lookup "in progress" until the test completes it
    public TaskCompletionSource<LocationResult>? Pending { get; set; }

    public Task<LocationResult> GetCurrentAsync()
    {
        Calls++;
        return Pending is { } pending ? pending.Task : Task.FromResult(Result);
    }
}
