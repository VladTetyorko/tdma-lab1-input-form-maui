using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class SessionStoreTests
{
    private readonly FakeSecretStore _secrets = new();
    private readonly FakeClock _clock = new();
    private readonly InMemoryUserRepository _users = new();
    private readonly List<LogRecord> _logs = [];
    private readonly SessionStore _session;
    private readonly User _user = new() { FullName = "Petro Bondarenko", Role = Role.Storekeeper, Login = "petro" };

    public SessionStoreTests()
    {
        _session = new SessionStore(_secrets, new HmacTokenService(_secrets, _clock), _users,
            new CapturingLogger<SessionStore>(_logs));
        _users.AddAsync(_user).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Save_ThenLoad_ReturnsTheSameUser()
    {
        await _session.SaveAsync(_user);

        Assert.True(await _session.HasValidSessionAsync());
        Assert.Same(_user, await _session.LoadUserAsync());
    }

    [Fact]
    public async Task Save_StoresOneTokenUnderOneKey_NotTheUserId()
    {
        await _session.SaveAsync(_user);

        Assert.DoesNotContain(_user.Id.ToString(), _secrets.Values[SessionStore.TokenKey]);
        Assert.Equal(2, _secrets.Values.Count);
    }

    [Fact]
    public async Task Load_NothingSaved_ReturnsNull()
    {
        Assert.Null(await _session.LoadUserAsync());
        Assert.False(await _session.HasValidSessionAsync());
    }

    [Fact]
    public async Task Clear_RemovesTheToken()
    {
        await _session.SaveAsync(_user);

        await _session.ClearAsync();

        Assert.False(_secrets.Values.ContainsKey(SessionStore.TokenKey));
        Assert.Null(await _session.LoadUserAsync());
    }

    [Fact]
    public async Task Load_TamperedToken_ReturnsNull_RemovesIt_AndLogsAWarningWithoutTheToken()
    {
        await _session.SaveAsync(_user);
        var token = _secrets.Values[SessionStore.TokenKey];
        _secrets.Values[SessionStore.TokenKey] = TokenTamper.ChangeSignature(token);

        Assert.Null(await _session.LoadUserAsync());

        Assert.False(_secrets.Values.ContainsKey(SessionStore.TokenKey));
        Assert.Contains(_logs, l => l.Level == LogLevel.Warning);
        Assert.All(_logs, l => Assert.DoesNotContain(token[..10], l.Message));
    }

    [Fact]
    public async Task Load_ExpiredToken_ReturnsNull()
    {
        await _session.SaveAsync(_user);

        _clock.UtcNow += HmacTokenService.Lifetime + TimeSpan.FromDays(1);

        Assert.Null(await _session.LoadUserAsync());
        Assert.False(_secrets.Values.ContainsKey(SessionStore.TokenKey));
    }

    [Fact]
    public async Task Load_UserNoLongerExists_ReturnsNull_AndRemovesTheToken()
    {
        await _session.SaveAsync(new User { FullName = "Ghost", Role = Role.Driver });

        Assert.Null(await _session.LoadUserAsync());
        Assert.False(_secrets.Values.ContainsKey(SessionStore.TokenKey));
    }

    [Fact]
    public async Task Logs_NeverContainTheToken()
    {
        await _session.SaveAsync(_user);
        var token = _secrets.Values[SessionStore.TokenKey];
        await _session.LoadUserAsync();
        await _session.ClearAsync();

        Assert.NotEmpty(_logs);
        Assert.All(_logs, l => Assert.DoesNotContain(token, l.Message));
        Assert.All(_logs, l => Assert.DoesNotContain(_secrets.Values[HmacTokenService.KeyName], l.Message));
    }
}
