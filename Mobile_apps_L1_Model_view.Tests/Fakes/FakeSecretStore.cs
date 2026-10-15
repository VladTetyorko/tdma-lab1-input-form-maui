using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Secrets in a dictionary. Failing = true makes every call throw, like unavailable secure storage
public sealed class FakeSecretStore : ISecretStore
{
    public Dictionary<string, string> Values { get; } = [];

    public bool Failing { get; set; }

    public Task<string?> GetAsync(string key)
    {
        ThrowIfFailing();
        return Task.FromResult(Values.GetValueOrDefault(key));
    }

    public Task SetAsync(string key, string value)
    {
        ThrowIfFailing();
        Values[key] = value;
        return Task.CompletedTask;
    }

    private void ThrowIfFailing()
    {
        if (Failing)
        {
            throw new InvalidOperationException("Secure storage is unavailable.");
        }
    }
}
