namespace Mobile_apps_L1_Model_view.Services;

// Fingerprint / face / Windows Hello behind one interface. It only answers "is this the device owner":
// the session itself is restored from the stored token, never from this answer alone
public interface IBiometricAuthenticator
{
    // Hardware present and something enrolled. Never throws
    Task<bool> IsAvailableAsync();

    // Shows the system prompt with the reason. Never throws: a platform error is NotAvailable
    Task<BiometricOutcome> AuthenticateAsync(string reason);
}
