using System.Security.Cryptography;
using System.Text;

namespace Mobile_apps_L1_Model_view.Services;

// Token = base64url(payload) + "." + base64url(HMAC-SHA256(key, base64url(payload))), payload = "userId.expiryUnixSeconds".
// The 32-byte signing key is generated once and kept in ISecretStore. The token is never logged
public sealed class HmacTokenService(ISecretStore secrets, IClock clock) : ITokenService
{
    public const string KeyName = "stockroom.token-key.v1";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private const int KeyLength = 32;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private byte[]? _key;

    public async Task<string> IssueAsync(Guid userId)
    {
        var key = (await GetKeyAsync(createIfMissing: true))!;
        var expires = new DateTimeOffset(clock.UtcNow + Lifetime).ToUnixTimeSeconds();
        var payload = Base64Url(Encoding.UTF8.GetBytes($"{userId:N}.{expires}"));
        return $"{payload}.{Base64Url(HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(payload)))}";
    }

    public async Task<Guid?> ValidateAsync(string token)
    {
        var parts = token?.Split('.');
        if (parts is not { Length: 2 } || await GetKeyAsync(createIfMissing: false) is not { } key)
        {
            return null;
        }

        if (!TryDecode(parts[1], out var signature)
            || !CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(parts[0])), signature)
            || !TryDecode(parts[0], out var payload))
        {
            return null;
        }

        var fields = Encoding.UTF8.GetString(payload).Split('.');
        if (fields.Length != 2 || !Guid.TryParseExact(fields[0], "N", out var userId)
            || !long.TryParse(fields[1], out var expires)
            || expires <= new DateTimeOffset(clock.UtcNow).ToUnixTimeSeconds())
        {
            return null;
        }

        return userId;
    }

    private async Task<byte[]?> GetKeyAsync(bool createIfMissing)
    {
        if (_key is not null)
        {
            return _key;
        }

        await _gate.WaitAsync();
        try
        {
            if (_key is null && await secrets.GetAsync(KeyName) is { } stored)
            {
                _key = Convert.FromBase64String(stored);
            }

            if (_key is null && createIfMissing)
            {
                var created = RandomNumberGenerator.GetBytes(KeyLength);
                await secrets.SetAsync(KeyName, Convert.ToBase64String(created));
                _key = created;
            }

            return _key;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryDecode(string text, out byte[] data)
    {
        var padded = text.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        try
        {
            data = Convert.FromBase64String(padded);
            return true;
        }
        catch (FormatException)
        {
            data = [];
            return false;
        }
    }
}
