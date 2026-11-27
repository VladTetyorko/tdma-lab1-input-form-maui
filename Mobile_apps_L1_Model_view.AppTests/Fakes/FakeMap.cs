namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeMap : IMap
{
    public Exception? Throws { get; set; }

    public List<(double Latitude, double Longitude, string? Name)> Opened { get; } = [];

    public Task OpenAsync(double latitude, double longitude, MapLaunchOptions options)
    {
        if (Throws is not null)
        {
            throw Throws;
        }

        Opened.Add((latitude, longitude, options.Name));
        return Task.CompletedTask;
    }

    public Task OpenAsync(Location location, MapLaunchOptions options) =>
        OpenAsync(location.Latitude, location.Longitude, options);

    public Task OpenAsync(Placemark placemark, MapLaunchOptions options) => throw new NotSupportedException();

    public Task<bool> TryOpenAsync(double latitude, double longitude, MapLaunchOptions options) =>
        throw new NotSupportedException();

    public Task<bool> TryOpenAsync(Location location, MapLaunchOptions options) => throw new NotSupportedException();

    public Task<bool> TryOpenAsync(Placemark placemark, MapLaunchOptions options) => throw new NotSupportedException();
}
