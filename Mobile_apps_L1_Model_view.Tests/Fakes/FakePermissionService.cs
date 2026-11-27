using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Answers EnsureAsync with a fixed result per feature (Granted unless set) and remembers what was asked
public sealed class FakePermissionService : IPermissionService
{
    public Dictionary<DeviceFeature, PermissionResult> Results { get; } = [];

    public List<DeviceFeature> Asked { get; } = [];

    public Task<PermissionResult> EnsureAsync(DeviceFeature feature)
    {
        Asked.Add(feature);
        return Task.FromResult(Results.GetValueOrDefault(feature, PermissionResult.Granted));
    }
}
