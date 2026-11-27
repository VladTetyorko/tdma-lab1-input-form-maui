using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class Pbkdf2PasswordHasherTests
{
    // Low iteration count: only the format and the behaviour are tested here, not the cost
    private readonly Pbkdf2PasswordHasher _hasher = new(iterations: 1000);

    [Fact]
    public void Verify_KnownPbkdf2Sha256Vector_Matches()
    {
        // RFC 7914 / widely published vector: PBKDF2-HMAC-SHA256("password", "salt", 1, 32)
        var hash = "pbkdf2-sha256$1$" + Convert.ToBase64String("salt"u8.ToArray()) + "$"
                   + Convert.ToBase64String(Convert.FromHexString("120fb6cffcf8b32c43e7225256c4f837a86548c92ccc35480805987cb70be17b"));

        Assert.True(_hasher.Verify("password", hash));
        Assert.False(_hasher.Verify("Password", hash));
    }

    [Fact]
    public void Hash_HasTheDocumentedFormat()
    {
        var parts = _hasher.Hash("secret").Split('$');

        Assert.Equal(4, parts.Length);
        Assert.Equal("pbkdf2-sha256", parts[0]);
        Assert.Equal("1000", parts[1]);
        Assert.Equal(16, Convert.FromBase64String(parts[2]).Length);
        Assert.Equal(32, Convert.FromBase64String(parts[3]).Length);
    }

    [Fact]
    public void Hash_DoesNotContainThePassword()
    {
        Assert.DoesNotContain("hunter2-password", _hasher.Hash("hunter2-password"));
    }

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentStrings()
    {
        Assert.NotEqual(_hasher.Hash("secret"), _hasher.Hash("secret"));
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        Assert.True(_hasher.Verify("secret", _hasher.Hash("secret")));
    }

    [Theory]
    [InlineData("Secret")]
    [InlineData("secret ")]
    [InlineData("")]
    public void Verify_WrongPassword_ReturnsFalse(string attempt)
    {
        Assert.False(_hasher.Verify(attempt, _hasher.Hash("secret")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("pbkdf2-sha256$1000$AAAA")]
    [InlineData("md5$1000$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$abc$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$0$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$99999999999$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$1000$not base64!$AAAA")]
    [InlineData("pbkdf2-sha256$1000$AAAA$")]
    public void Verify_MalformedHash_ReturnsFalseWithoutThrowing(string hash)
    {
        Assert.False(_hasher.Verify("secret", hash));
        Assert.True(_hasher.NeedsRehash(hash));
    }

    [Fact]
    public void Verify_UsesTheIterationsStoredInTheHash()
    {
        var old = new Pbkdf2PasswordHasher(iterations: 10).Hash("secret");

        Assert.True(_hasher.Verify("secret", old));
    }

    [Fact]
    public void NeedsRehash_FewerIterationsThanConfigured_ReturnsTrue()
    {
        var old = new Pbkdf2PasswordHasher(iterations: 10).Hash("secret");

        Assert.True(_hasher.NeedsRehash(old));
        Assert.False(_hasher.NeedsRehash(_hasher.Hash("secret")));
    }

    [Fact]
    public void DefaultIterations_MeetTheSecurityRule()
    {
        Assert.True(Pbkdf2PasswordHasher.DefaultIterations >= 210_000);
        Assert.Contains("$210000$", new Pbkdf2PasswordHasher().Hash("secret"));
    }

    [Fact]
    public void Constructor_NonPositiveIterations_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pbkdf2PasswordHasher(0));
    }
}
