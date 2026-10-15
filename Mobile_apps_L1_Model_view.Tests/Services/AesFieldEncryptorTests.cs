using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class AesFieldEncryptorTests
{
    private readonly FakeSecretStore _secrets = new();

    private AesFieldEncryptor Create() => new(_secrets);

    [Fact]
    public async Task Decrypt_ReturnsWhatWasEncrypted()
    {
        var encryptor = Create();

        var cipher = await encryptor.EncryptAsync("Acme Ltd, Kyiv");

        Assert.Equal("Acme Ltd, Kyiv", await encryptor.DecryptAsync(cipher));
    }

    [Fact]
    public async Task Encrypt_HidesThePlainText_AndUsesAFreshIvEveryTime()
    {
        var encryptor = Create();

        var first = await encryptor.EncryptAsync("Acme Ltd");
        var second = await encryptor.EncryptAsync("Acme Ltd");

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("Acme", first);
    }

    [Fact]
    public async Task Encrypt_CreatesTheKeyOnce_AndANewInstanceReusesIt()
    {
        var cipher = await Create().EncryptAsync("Acme Ltd");
        var keyAfterFirst = _secrets.Values[AesFieldEncryptor.KeyName];

        var text = await Create().DecryptAsync(cipher);

        Assert.Equal("Acme Ltd", text);
        Assert.Equal(keyAfterFirst, _secrets.Values[AesFieldEncryptor.KeyName]);
    }

    [Fact]
    public async Task Decrypt_WithADifferentKey_ThrowsEncryptionException()
    {
        var cipher = await Create().EncryptAsync("Acme Ltd");
        _secrets.Values.Clear();
        await Create().EncryptAsync("other"); // creates a new key

        var ex = await Assert.ThrowsAsync<EncryptionException>(() => Create().DecryptAsync(cipher));

        Assert.Contains("different key", ex.Message);
    }

    [Fact]
    public async Task Decrypt_WithMissingKey_ThrowsAndDoesNotCreateOne()
    {
        var cipher = await Create().EncryptAsync("Acme Ltd");
        _secrets.Values.Clear();

        var ex = await Assert.ThrowsAsync<EncryptionException>(() => Create().DecryptAsync(cipher));

        Assert.Contains("key is missing", ex.Message);
        Assert.Empty(_secrets.Values);
    }

    [Fact]
    public async Task Decrypt_ChangedText_ThrowsEncryptionException()
    {
        var encryptor = Create();
        var bytes = Convert.FromBase64String(await encryptor.EncryptAsync("Acme Ltd"));
        bytes[20] ^= 0xFF;

        await Assert.ThrowsAsync<EncryptionException>(() => encryptor.DecryptAsync(Convert.ToBase64String(bytes)));
    }

    [Theory]
    [InlineData("not base64 at all!")]
    [InlineData("AAAA")]
    public async Task Decrypt_NotEncryptedText_ThrowsEncryptionException(string text)
    {
        var encryptor = Create();
        await encryptor.EncryptAsync("x");

        await Assert.ThrowsAsync<EncryptionException>(() => encryptor.DecryptAsync(text));
    }

    [Fact]
    public async Task Encrypt_SecureStorageUnavailable_ThrowsEncryptionException()
    {
        _secrets.Failing = true;

        await Assert.ThrowsAsync<EncryptionException>(() => Create().EncryptAsync("Acme Ltd"));
    }
}
