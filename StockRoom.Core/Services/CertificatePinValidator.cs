using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class CertificatePinValidator : ICertificatePinValidator
{
    private const string Prefix = "sha256/";

    private readonly HashSet<string> _pins;

    // A pin may be written "sha256/<base64>" (the common HPKP form) or as plain base64
    public CertificatePinValidator(IEnumerable<string> pins)
    {
        ArgumentNullException.ThrowIfNull(pins);
        _pins = pins
            .Select(p => p.Trim())
            .Select(p => p.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) ? p[Prefix.Length..] : p)
            .Where(p => p.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    public bool HasPins => _pins.Count > 0;

    public bool IsPinned(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        return _pins.Contains(ComputePin(certificate));
    }

    // What to put in appsettings.local.json for this certificate
    public static string ComputePin(X509Certificate2 certificate) =>
        Convert.ToBase64String(SHA256.HashData(certificate.PublicKey.ExportSubjectPublicKeyInfo()));
}
