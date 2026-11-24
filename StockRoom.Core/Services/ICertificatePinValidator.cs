using System.Security.Cryptography.X509Certificates;

namespace Mobile_apps_L1_Model_view.Services;

// Certificate pinning: the server certificate is accepted only when its public key is one the app was told about.
// A pin is the base64 SHA-256 of the certificate SubjectPublicKeyInfo, so it survives certificate renewal
// with the same key. Pure logic: no network, no platform
public interface ICertificatePinValidator
{
    // False when no pin is configured: pinned mode with nothing to pin to must refuse, not allow everything
    bool HasPins { get; }

    bool IsPinned(X509Certificate2 certificate);
}
