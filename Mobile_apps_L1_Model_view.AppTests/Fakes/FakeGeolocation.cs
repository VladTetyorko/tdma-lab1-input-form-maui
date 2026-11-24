namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// IGeolocation without a GPS: a fresh fix, a last known fix, or an exception, each set by the test
public sealed class FakeGeolocation : IGeolocation
{
    public Location? Fresh { get; set; } = new(50.4501, 30.5234);

    public Location? LastKnown { get; set; }

    // Thrown by GetLocationAsync (GPS off, no permission, ...)
    public Exception? FreshThrows { get; set; }

    public GeolocationRequest? LastRequest { get; private set; }

    public int FreshCalls { get; private set; }

    public int LastKnownCalls { get; private set; }

    public bool IsListeningForeground => false;

    public bool IsEnabled => true;

    public Task<Location?> GetLastKnownLocationAsync()
    {
        LastKnownCalls++;
        return Task.FromResult(LastKnown);
    }

    public Task<Location?> GetLocationAsync(GeolocationRequest? request, CancellationToken cancelToken)
    {
        FreshCalls++;
        LastRequest = request;
        if (FreshThrows is not null)
        {
            throw FreshThrows;
        }

        return Task.FromResult(Fresh);
    }

    public Task<bool> StartListeningForegroundAsync(GeolocationListeningRequest request) => Task.FromResult(false);

    public void StopListeningForeground()
    {
    }

#pragma warning disable CS0067 // part of the interface, not used by the tests
    public event EventHandler<GeolocationLocationChangedEventArgs>? LocationChanged;

    public event EventHandler<GeolocationListeningFailedEventArgs>? ListeningFailed;
#pragma warning restore CS0067
}
