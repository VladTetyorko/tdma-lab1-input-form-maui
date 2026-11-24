using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeMapLauncher : IMapLauncher
{
    public DeviceStatus Result { get; set; } = DeviceStatus.Succeeded;

    public List<(GeoPoint Location, string Label)> Opened { get; } = [];

    public Task<DeviceStatus> OpenAsync(GeoPoint location, string label)
    {
        Opened.Add((location, label));
        return Task.FromResult(Result);
    }
}
