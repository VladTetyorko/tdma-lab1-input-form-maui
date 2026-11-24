using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class HmacTokenServiceTests
{
    private readonly FakeSecretStore _secrets = new();
    private readonly FakeClock _clock = new();
    private readonly HmacTokenService _tokens;

    public HmacTokenServiceTests()
    {
        _tokens = new HmacTokenService(_secrets, _clock);
    }

    [Fact]
    public async Task IssuedToken_Validates_ToTheSameUser()
    {
        var userId = Guid.NewGuid();

        var token = await _tokens.IssueAsync(userId);

        Assert.Equal(userId, await _tokens.ValidateAsync(token));
    }

    [Fact]
    public async Task Token_HasTwoBase64UrlParts_AndNoPadding()
    {
        var token = await _tokens.IssueAsync(Guid.NewGuid());

        var parts = token.Split('.');
        Assert.Equal(2, parts.Length);
        Assert.DoesNotContain(token, c => c is '=' or '+' or '/');
    }

    [Fact]
    public async Task SigningKey_IsCreatedOnce_AndKeptInTheSecretStore()
    {
        await _tokens.IssueAsync(Guid.NewGuid());
        var key = _secrets.Values[HmacTokenService.KeyName];
        await _tokens.IssueAsync(Guid.NewGuid());

        Assert.Equal(key, _secrets.Values[HmacTokenService.KeyName]);
        Assert.Equal(32, Convert.FromBase64String(key).Length);
    }

    [Fact]
    public async Task Token_SurvivesAnAppRestart_BecauseTheKeyIsInTheSecretStore()
    {
        var userId = Guid.NewGuid();
        var token = await _tokens.IssueAsync(userId);

        var restarted = new HmacTokenService(_secrets, _clock);

        Assert.Equal(userId, await restarted.ValidateAsync(token));
    }

    [Fact]
    public async Task ChangedPayload_IsRejected()
    {
        var token = await _tokens.IssueAsync(Guid.NewGuid());
        var other = await _tokens.IssueAsync(Guid.NewGuid());
        var forged = other.Split('.')[0] + "." + token.Split('.')[1];

        Assert.Null(await _tokens.ValidateAsync(forged));
    }

    [Fact]
    public async Task ChangedSignature_IsRejected()
    {
        var token = await _tokens.IssueAsync(Guid.NewGuid());

        Assert.Null(await _tokens.ValidateAsync(TokenTamper.ChangeSignature(token)));
    }

    [Fact]
    public async Task TokenFromAnotherKey_IsRejected()
    {
        var token = await _tokens.IssueAsync(Guid.NewGuid());
        var otherDevice = new HmacTokenService(new FakeSecretStore(), _clock);
        await otherDevice.IssueAsync(Guid.NewGuid());

        Assert.Null(await otherDevice.ValidateAsync(token));
    }

    [Fact]
    public async Task Token_IsRejectedAfterThirtyDays_ButNotBefore()
    {
        var token = await _tokens.IssueAsync(Guid.NewGuid());

        _clock.UtcNow += HmacTokenService.Lifetime - TimeSpan.FromMinutes(1);
        Assert.NotNull(await _tokens.ValidateAsync(token));

        _clock.UtcNow += TimeSpan.FromMinutes(2);
        Assert.Null(await _tokens.ValidateAsync(token));
        Assert.Equal(TimeSpan.FromDays(30), HmacTokenService.Lifetime);
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("a.b.c")]
    [InlineData("!!!.???")]
    [InlineData(".")]
    public async Task MalformedToken_IsRejectedWithoutThrowing(string token)
    {
        await _tokens.IssueAsync(Guid.NewGuid());

        Assert.Null(await _tokens.ValidateAsync(token));
    }

    [Fact]
    public async Task Validate_WithoutAnyKey_ReturnsNull_AndDoesNotCreateOne()
    {
        Assert.Null(await _tokens.ValidateAsync("abc.def"));
        Assert.Empty(_secrets.Values);
    }
}
