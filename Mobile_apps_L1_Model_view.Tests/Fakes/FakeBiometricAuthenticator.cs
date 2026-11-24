using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

public sealed class FakeBiometricAuthenticator : IBiometricAuthenticator
{
    public bool Available { get; set; } = true;

    public BiometricOutcome Outcome { get; set; } = BiometricOutcome.Success;

    public List<string> Reasons { get; } = [];

    public Task<bool> IsAvailableAsync() => Task.FromResult(Available);

    public Task<BiometricOutcome> AuthenticateAsync(string reason)
    {
        Reasons.Add(reason);
        return Task.FromResult(Outcome);
    }
}
