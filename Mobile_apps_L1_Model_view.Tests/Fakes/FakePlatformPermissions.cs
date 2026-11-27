using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// The system permission state per feature; RequestAsync answers with RequestResult and may throw
public sealed class FakePlatformPermissions : IPlatformPermissions
{
    // What CheckAsync reports; a feature that is not listed is Granted
    public Dictionary<DeviceFeature, PermissionResult> State { get; } = [];

    // What the system prompt answers (and then State follows it)
    public PermissionResult RequestResult { get; set; } = PermissionResult.Granted;

    public Exception? Throws { get; set; }

    public List<DeviceFeature> Requested { get; } = [];

    public Task<PermissionResult> CheckAsync(DeviceFeature feature)
    {
        if (Throws is not null)
        {
            throw Throws;
        }

        return Task.FromResult(State.GetValueOrDefault(feature, PermissionResult.Granted));
    }

    public Task<PermissionResult> RequestAsync(DeviceFeature feature)
    {
        if (Throws is not null)
        {
            throw Throws;
        }

        Requested.Add(feature);
        State[feature] = RequestResult;
        return Task.FromResult(RequestResult);
    }
}
