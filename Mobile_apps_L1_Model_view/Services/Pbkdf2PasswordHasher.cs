using System.Security.Cryptography;
using System.Text;

namespace Mobile_apps_L1_Model_view.Services;

// PBKDF2-HMAC-SHA256 with a random 16-byte salt. Stored as "pbkdf2-sha256$iterations$saltBase64$hashBase64",
// so the iteration count can be raised later and old hashes still verify (and report NeedsRehash)
public sealed class Pbkdf2PasswordHasher(int iterations = Pbkdf2PasswordHasher.DefaultIterations) : IPasswordHasher
{
    public const int DefaultIterations = 210_000;
    public const string Prefix = "pbkdf2-sha256";

    private const int SaltLength = 16;
    private const int HashLength = 32;
    private const int MaxIterations = 10_000_000;

    private readonly int _iterations = iterations > 0
        ? iterations
        : throw new ArgumentOutOfRangeException(nameof(iterations));

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Derive(password, salt, _iterations, HashLength);
        return $"{Prefix}${_iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        if (password is null || !TryParse(hash, out var iterationCount, out var salt, out var expected))
        {
            return false;
        }

        var actual = Derive(password, salt, iterationCount, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public bool NeedsRehash(string hash) =>
        !TryParse(hash, out var iterationCount, out _, out _) || iterationCount < _iterations;

    private static byte[] Derive(string password, byte[] salt, int iterationCount, int length) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterationCount, HashAlgorithmName.SHA256, length);

    private static bool TryParse(string? hash, out int iterationCount, out byte[] salt, out byte[] expected)
    {
        iterationCount = 0;
        salt = [];
        expected = [];

        var parts = hash?.Split('$');
        if (parts is not { Length: 4 } || parts[0] != Prefix
            || !int.TryParse(parts[1], out iterationCount) || iterationCount is < 1 or > MaxIterations)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length > 0 && expected.Length > 0;
    }
}
