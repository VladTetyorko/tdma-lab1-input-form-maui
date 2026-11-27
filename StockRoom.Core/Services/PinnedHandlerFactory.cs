using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Mobile_apps_L1_Model_view.Services;

// The HttpClientHandler of our own sync client. Pinning is an addition to the normal certificate check, never a
// replacement: a certificate that fails the chain check (expired, wrong host, untrusted issuer) is refused even if its key
// matches a pin. Only used for our own server, never for third-party hosts such as the open databases
public static class PinnedHandlerFactory
{
    public static HttpClientHandler Create(ICertificatePinValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, certificate, _, errors) => Accepts(validator, certificate, errors)
        };
    }

    // errors: what the platform found wrong with the chain and host name (None = the normal check passed)
    public static bool Accepts(ICertificatePinValidator validator, X509Certificate2? certificate, SslPolicyErrors errors) =>
        errors == SslPolicyErrors.None
        && certificate is not null
        && validator.HasPins
        && validator.IsPinned(certificate);
}
